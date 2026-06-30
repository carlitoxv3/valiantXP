using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ValiantXP.Application.DTOs;
using ValiantXP.Application.Features.Rally.Commands;
using ValiantXP.Application.Features.Rally.Queries;

namespace ValiantXP.API.Controllers;

/// <summary>
/// Rally module endpoints — UGC competition gallery, voting, and admin moderation/winner selection.
///
/// Public endpoints (gallery, vote): require authentication.
/// Admin endpoints (moderate, select-winners): require Admin role.
///
/// Submit Rally: use POST /api/dynamics/{id}/submit (existing DynamicsController).
/// </summary>
[ApiController]
[Route("api/rally")]
[Produces("application/json")]
public sealed class RallyController : ControllerBase
{
    private readonly ISender _sender;

    public RallyController(ISender sender)
    {
        _sender = sender;
    }

    // ─── Gallery ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the public gallery of approved Rally submissions for a challenge.
    /// Ordered by vote count descending (most popular first).
    /// </summary>
    /// <param name="challengeId">The Rally DynamicChallenge ID.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page, max 50 (default 20).</param>
    /// <response code="200">Gallery page returned successfully.</response>
    /// <response code="401">Authentication required.</response>
    [HttpGet("{challengeId:guid}/submissions")]
    [Authorize]
    [ProducesResponseType(typeof(IList<RallySubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGallery(
        Guid challengeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await _sender.Send(new GetRallySubmissionsQuery(challengeId, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // ─── Voting ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Casts a community vote on an approved Rally submission.
    /// One vote per user per submission. Max votes per day configured per campaign.
    /// </summary>
    /// <param name="submissionId">The submission ID to vote for.</param>
    /// <response code="200">Vote registered successfully.</response>
    /// <response code="400">Already voted, submission not approved, or daily limit reached.</response>
    /// <response code="401">Authentication required.</response>
    [HttpPost("submissions/{submissionId:guid}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(VoteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Vote(Guid submissionId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _sender.Send(new VoteRallySubmissionCommand(submissionId, userId, remoteIp), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // ─── Admin: Moderation ────────────────────────────────────────────────────

    /// <summary>
    /// [Admin] Reviews and moderates a pending Rally submission.
    /// Transitions status to Approved, Rejected, or Banned.
    /// </summary>
    /// <param name="submissionId">The submission ID to moderate.</param>
    /// <param name="request">Decision and optional notes.</param>
    /// <response code="200">Moderation applied successfully.</response>
    /// <response code="400">Invalid decision or submission already moderated.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpPost("submissions/{submissionId:guid}/moderate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ModerationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Moderate(
        Guid submissionId,
        [FromBody] ModerateSubmissionRequestDto request,
        CancellationToken ct = default)
    {
        var moderatorId = GetCurrentUserId();
        if (moderatorId == Guid.Empty) return Unauthorized();

        var result = await _sender.Send(
            new ModerateRallySubmissionCommand(submissionId, moderatorId, request.Decision, request.Notes), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// [Admin] Returns the moderation queue — all pending submissions for a challenge.
    /// </summary>
    [HttpGet("{challengeId:guid}/submissions/pending")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IList<RallySubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingQueue(Guid challengeId, CancellationToken ct = default)
    {
        // Note: For the pending queue we reuse the gallery query with status filter
        // A dedicated query can be added in a future sprint for performance.
        var result = await _sender.Send(new GetRallySubmissionsQuery(challengeId, 1, 100), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // ─── Admin: Winner Selection ───────────────────────────────────────────────

    /// <summary>
    /// [Admin] Selects winners for a Rally challenge.
    /// Triggers prize assignment via RallyWinnerSelectedEvent → ChallengeCompletedEvent pipeline.
    ///
    /// Supported modes (configured in DynamicChallenge.AntiFraudConfigJson.Rally.WinnerSelectionMode):
    ///   - ByAdmin: provide explicit WinnerSubmissionIds
    ///   - ByVotes: top N by vote count (automatic)
    ///   - ByLottery: random N from approved submissions (automatic)
    ///   - ByTicketAmount: highest ticket total (automatic)
    /// </summary>
    /// <param name="challengeId">The Rally DynamicChallenge ID.</param>
    /// <param name="request">For ByAdmin mode, list of winner submission IDs.</param>
    /// <response code="200">Winners selected and prizes assigned.</response>
    /// <response code="400">No approved submissions or invalid configuration.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpPost("{challengeId:guid}/select-winners")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(WinnerSelectionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SelectWinners(
        Guid challengeId,
        [FromBody] SelectWinnersRequestDto request,
        CancellationToken ct = default)
    {
        var adminId = GetCurrentUserId();
        if (adminId == Guid.Empty) return Unauthorized();

        var result = await _sender.Send(
            new SelectRallyWinnersCommand(challengeId, adminId, request.WinnerSubmissionIds), ct);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
