using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ValiantXP.Application.DTOs;
using ValiantXP.Application.Features.Dynamics.Commands;
using ValiantXP.Application.Features.Dynamics.Queries;
using ValiantXP.Application.Features.Dynamics.Rally.Commands;
using ValiantXP.Application.Features.Dynamics.Rally.Queries;

namespace ValiantXP.API.Controllers;

/// <summary>
/// Unified Dynamics controller.
///
/// Handles generic challenge operations for ALL dynamic types (Trivia, Survey, Code, Rally),
/// plus Rally-specific lifecycle endpoints (gallery, voting, moderation, winner selection)
/// under the same resource path /api/dynamics/{id}/... since Rally IS a DynamicType.
///
/// Anti-fraud error codes returned in 400/422/429 responses:
///   CAMPAIGN_NOT_ACTIVE, CODE_NOT_FOUND, CODE_ALREADY_USED, DAILY_LIMIT_EXCEEDED,
///   IP_LIMIT_EXCEEDED, USER_BLOCKED, TRIVIA_ATTEMPTS_EXCEEDED, SURVEY_ALREADY_ANSWERED,
///   RALLY_SUBMISSION_LIMIT, RALLY_DUPLICATE_TICKET
/// </summary>
[Authorize]
[ApiController]
[Route("api/dynamics")]
[Produces("application/json")]
public sealed class DynamicsController : ControllerBase
{
    private readonly ISender _sender;

    public DynamicsController(ISender sender) => _sender = sender;

    // ══════════════════════════════════════════════════════════════════
    // GENERIC — All dynamic types
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Get a challenge by ID (sanitized — correct answers are stripped server-side).</summary>
    /// <response code="200">Challenge details returned.</response>
    /// <response code="404">Challenge not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge(Guid id)
    {
        var result = await _sender.Send(new GetChallengeQuery(id));
        if (!result.IsSuccess)
            return result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    /// <summary>
    /// Submit a dynamic challenge (Trivia, Survey, Code, or Rally).
    /// Runs the full anti-fraud pipeline before executing the strategy.
    ///
    /// Inputs by type:
    ///   Trivia  → { "q1": "B", "q2": "A" }
    ///   Survey  → { "opinion": "Great product" }
    ///   Code    → { "code": "PROMO2026" }
    ///   Rally   → { "mediaUrl": "https://...", "subChallengeTag": "{\"challengeId\":1}" }
    ///             (returns PendingModeration — prize assigned after winner selection)
    /// </summary>
    /// <response code="200">Submission processed. Check `success` field for the result.</response>
    /// <response code="422">Anti-fraud rule rejection.</response>
    /// <response code="429">Rate limit exceeded.</response>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SubmitChallenge(Guid id, [FromBody] SubmitChallengeRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _sender.Send(new SubmitChallengeCommand(id, userId, dto.Inputs, remoteIp));

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    // ══════════════════════════════════════════════════════════════════
    // RALLY lifecycle — DynamicType.Rally only
    // These endpoints extend the generic submit flow with the Rally
    // competition lifecycle: validate → submit → gallery → vote →
    // moderate → select winners.
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// [Rally] Pre-validates user access to a Rally challenge.
    /// Returns available sub-challenge and remaining submission quota.
    /// Call this before showing the submission form to the user.
    /// Mirrors PromoHub's /rallies/validate (Chatbot) and /rally/{id}/validate (User API).
    /// </summary>
    /// <response code="200">Validation result including CanSubmit, RemainingSubmissions, AvailableSubChallenge.</response>
    /// <response code="400">Rally not active, campaign inactive, or limit already exceeded.</response>
    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(typeof(RallyAccessValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateRallyAccess(Guid id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _sender.Send(new ValidateRallyAccessCommand(id, userId, remoteIp), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// [Rally] Returns rally info and live stats (approved count, winner count, hasWinners flag).
    /// Combines PromoHub's GET /rally/active + GET /rally/get?id into one endpoint.
    /// </summary>
    [HttpGet("{id:guid}/info")]
    [ProducesResponseType(typeof(RallyInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRallyInfo(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetRallyInfoQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// [Rally] Public gallery of approved submissions, ordered by vote count (most popular first).
    /// Mirrors PromoHub's GET /rally/getforuser + gallery views.
    /// </summary>
    /// <param name="id">Rally DynamicChallenge ID.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page, max 50 (default 20).</param>
    [HttpGet("{id:guid}/submissions")]
    [ProducesResponseType(typeof(IList<RallySubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGallery(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await _sender.Send(new GetRallySubmissionsQuery(id, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// [Rally] Returns submissions of the authenticated user (all statuses).
    /// Lets users track their PendingModeration/Approved/Rejected submissions.
    /// Mirrors PromoHub's GET /rally/getforuser.
    /// </summary>
    [HttpGet("{id:guid}/my-submissions")]
    [ProducesResponseType(typeof(IList<RallySubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubmissions(Guid id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _sender.Send(new GetMyRallySubmissionsQuery(id, userId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// [Rally] Public winners board — submissions marked as IsWinner=true.
    /// Mirrors PromoHub's GET /rally/winners.
    /// </summary>
    [HttpGet("{id:guid}/submissions/winners")]
    [ProducesResponseType(typeof(IList<RallySubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWinners(Guid id, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetRallyWinnersQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// [Rally] Cast a community vote on an approved submission.
    /// One vote per user per submission. Daily limit configured per campaign.
    /// Mirrors PromoHub's POST /rally/vote.
    /// </summary>
    /// <param name="submissionId">The submission ID to vote for.</param>
    [HttpPost("submissions/{submissionId:guid}/vote")]
    [ProducesResponseType(typeof(VoteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Vote(Guid submissionId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _sender.Send(new VoteRallySubmissionCommand(submissionId, userId, remoteIp), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    // ── Admin: Moderation ──────────────────────────────────────────────

    /// <summary>
    /// [Admin][Rally] Moderation queue — all pending submissions for a challenge, FIFO ordered.
    /// Mirrors PromoHub Admin's POST /rally/multimedia/moderation/list.
    /// </summary>
    [HttpGet("{id:guid}/submissions/pending")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IList<RallySubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingQueue(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetRallyPendingQueueQuery(id, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// [Admin][Rally] Moderation item detail — full submission data including ticket/OCR info.
    /// Mirrors PromoHub Admin's GET /rally/multimedia/moderation/{id}.
    /// </summary>
    [HttpGet("submissions/{submissionId:guid}/moderation")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RallyModerationItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModerationItem(Guid submissionId, CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetRallyModerationItemQuery(submissionId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// [Admin][Rally] Apply a moderation decision to a pending submission.
    /// Transitions status to Approved, Rejected, or Banned.
    /// Mirrors PromoHub Admin's PUT /rally/{rallyId}/multimedia/status/{id}/{statusId}.
    /// </summary>
    [HttpPost("submissions/{submissionId:guid}/moderate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ModerationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    // ── Admin: Winner selection ────────────────────────────────────────

    /// <summary>
    /// [Admin][Rally] Selects winners and triggers prize assignment via event pipeline.
    ///
    /// Selection modes (set in DynamicChallenge.AntiFraudConfigJson.Rally):
    ///   ByAdmin   → provide explicit WinnerSubmissionIds in request body
    ///   ByVotes   → top N by vote count (automatic)
    ///   ByLottery → random N from approved submissions (automatic)
    ///   ByTicketAmount → highest ticket total (automatic)
    ///
    /// Fires: RallyWinnerSelectedEvent → ChallengeCompletedEvent → prize assignment.
    /// Mirrors PromoHub Admin's POST /rally/winners/{rallyId} (Excel upload → here it's API-driven).
    /// </summary>
    [HttpPost("{id:guid}/select-winners")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(WinnerSelectionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SelectWinners(
        Guid id,
        [FromBody] SelectWinnersRequestDto request,
        CancellationToken ct = default)
    {
        var adminId = GetCurrentUserId();
        if (adminId == Guid.Empty) return Unauthorized();

        var result = await _sender.Send(
            new SelectRallyWinnersCommand(id, adminId, request.WinnerSubmissionIds), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    // ── Private helpers ──────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
