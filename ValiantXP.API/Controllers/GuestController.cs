using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.API.Controllers;

/// <summary>Anonymous guest session management.</summary>
[ApiController]
[Route("api/guest")]
[Produces("application/json")]
public class GuestController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public GuestController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <summary>Create a new guest session for anonymous participation.</summary>
    [HttpPost("session")]
    [ProducesResponseType(typeof(object), 201)]
    public async Task<IActionResult> CreateSession([FromBody] CreateGuestSessionRequest request)
    {
        var ttl = request.TtlMinutes > 0 ? request.TtlMinutes : 60;
        var session = new GuestSession
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            ChannelHint = request.ChannelHint,
            ExternalHint = request.ExternalHint,
            TtlMinutes = ttl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ttl),
            ActiveChallengeId = request.ChallengeId
        };

        await _uow.GuestSessions.AddAsync(session);
        await _uow.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSession), new { token = session.Token }, new
        {
            token = session.Token,
            expiresAt = session.ExpiresAt,
            challengeId = session.ActiveChallengeId
        });
    }

    /// <summary>Get guest session info.</summary>
    [HttpGet("session/{token}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSession(string token)
    {
        var session = await _uow.GuestSessions.FindByTokenAsync(token);
        if (session is null) return NotFound();

        return Ok(new
        {
            token = session.Token,
            expiresAt = session.ExpiresAt,
            isExpired = session.IsExpired,
            isConverted = session.IsConverted,
            convertedAt = session.ConvertedAt
        });
    }

    /// <summary>Convert guest session to authenticated user (claim transfer).</summary>
    [HttpPost("convert")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Convert([FromBody] ConvertGuestSessionRequest request)
    {
        var session = await _uow.GuestSessions.FindByTokenAsync(request.GuestToken);
        if (session is null)
            return BadRequest(new { error = "Guest session not found" });
        if (session.IsExpired)
            return BadRequest(new { error = "Guest session has expired. Please restart your participation." });
        if (session.IsConverted)
            return BadRequest(new { error = "Guest session already converted" });

        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (userIdValue is null || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        // Claim transfer
        session.ConvertedToUserId = userId;
        session.ConvertedAt = DateTime.UtcNow;
        await _uow.GuestSessions.UpdateAsync(session);
        await _uow.SaveChangesAsync();

        return Ok(new
        {
            message = "Session converted successfully",
            progressJson = session.ProgressJson,
            challengeId = session.ActiveChallengeId
        });
    }
}

public record CreateGuestSessionRequest(
    IdentityProvider ChannelHint,
    string? ExternalHint,
    int TtlMinutes,
    Guid? ChallengeId
);

public record ConvertGuestSessionRequest(string GuestToken);
