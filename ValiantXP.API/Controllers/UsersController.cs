using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Features.Dynamics.Queries;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public UsersController(IUnitOfWork unitOfWork, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    /// <summary>Returns the authenticated user's profile.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _unitOfWork.Users.GetAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        // Get total points balance
        var pointBalance = await _mediator.Send(new GetUserPointBalanceQuery(userId));

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            displayName = !string.IsNullOrEmpty(user.UserName) ? user.UserName : user.Email.Split('@')[0],
            avatarUrl = (string?)null,
            totalPoints = pointBalance?.TotalPoints ?? 0,
            isMfaEnabled = user.IsMfaEnabled,
            createdAt = user.CreatedAt
        });
    }

    /// <summary>Returns all prizes awarded to the authenticated user.</summary>
    [HttpGet("prizes")]
    public async Task<IActionResult> GetMyPrizes(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var prizes = await _mediator.Send(new GetUserPrizesQuery(userId.Value), ct);
        return Ok(prizes);
    }

    /// <summary>Returns the authenticated user's point balance and recent movements.</summary>
    [HttpGet("points")]
    public async Task<IActionResult> GetMyPoints(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var balance = await _mediator.Send(new GetUserPointBalanceQuery(userId.Value), ct);
        return Ok(balance);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim != null && Guid.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>Returns all identity providers linked to the authenticated user.</summary>
    [HttpGet("me/identities")]
    public async Task<IActionResult> GetMyIdentities(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var identities = await _unitOfWork.UserIdentities.GetByUserAsync(userId.Value, ct);
        return Ok(identities.Select(i => new
        {
            id = i.Id,
            provider = i.Provider.ToString(),
            externalId = i.ExternalId,
            emailClaim = i.EmailClaim,
            isEmailVerified = i.IsEmailVerified,
            isPrimary = i.IsPrimary,
            isActive = i.IsActive,
            linkedAt = i.LinkedAt,
            lastSeenAt = i.LastSeenAt
        }));

    }

    /// <summary>Unlinks an identity provider from the authenticated user (D2 — requires at least one remaining).</summary>
    [HttpDelete("me/identities/{identityId:guid}")]
    public async Task<IActionResult> UnlinkIdentity(Guid identityId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var activeCount = await _unitOfWork.UserIdentities.CountActiveByUserAsync(userId.Value, ct);
        if (activeCount <= 1)
            return BadRequest(new { error = "Cannot unlink the only identity. Add another provider first." });

        var identities = await _unitOfWork.UserIdentities.GetByUserAsync(userId.Value, ct);
        var target = identities.FirstOrDefault(i => i.Id == identityId);
        if (target is null) return NotFound();

        // Soft delete — keep for audit trail
        target.IsActive = false;
        target.UnlinkedAt = DateTime.UtcNow;
        await _unitOfWork.UserIdentities.UpdateAsync(target, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { message = "Identity unlinked successfully" });
    }
}
