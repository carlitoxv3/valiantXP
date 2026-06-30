using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ValiantXP.Application.DTOs;
using ValiantXP.Application.Features.Dynamics.Commands;
using ValiantXP.Application.Features.Dynamics.Queries;

namespace ValiantXP.API.Controllers;

/// <summary>Dynamics controller — submit gamification challenges and retrieve challenge details.</summary>
[Authorize]
[ApiController]
[Route("api/dynamics")]
[Produces("application/json")]
public class DynamicsController : ControllerBase
{
    private readonly ISender _sender;

    public DynamicsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Get a challenge by ID.</summary>
    /// <remarks>
    /// Returns challenge details without revealing correct answers (safe for client display).
    /// Correct answers are stripped server-side before returning the response.
    /// </remarks>
    /// <param name="id">The challenge GUID.</param>
    /// <response code="200">Challenge details returned.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Unauthorized — JWT required.</response>
    /// <response code="404">Challenge not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge(Guid id)
    {
        var query = new GetChallengeQuery(id);
        var result = await _sender.Send(query);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { error = result.Error });
            }
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>Submit a dynamic challenge (Trivia, Survey, or Code redemption).</summary>
    /// <remarks>
    /// Executes the full anti-fraud pipeline before processing the dynamic strategy.
    /// On success, fires a domain event that may trigger InstantWin prize assignment.
    /// Returns `nextChallengeId` when the challenge is part of a chain.
    ///
    /// **Anti-fraud error codes returned in 400/422/429 responses:**
    /// - `CAMPAIGN_NOT_ACTIVE` — outside campaign date window
    /// - `CODE_NOT_FOUND` — submitted code does not exist
    /// - `CODE_ALREADY_USED` — code was already redeemed
    /// - `DAILY_LIMIT_EXCEEDED` — daily redemption cap reached
    /// - `IP_LIMIT_EXCEEDED` — too many requests from this IP
    /// - `USER_BLOCKED` — blocked after too many failed attempts (DetectBots)
    /// - `TRIVIA_ATTEMPTS_EXCEEDED` — max trivia attempts reached
    /// - `SURVEY_ALREADY_ANSWERED` — survey already submitted
    ///
    /// **Inputs by dynamic type:**
    /// - Trivia: `{ "q1": "B", "q2": "A" }`
    /// - Survey: `{ "opinion": "Great product" }`
    /// - Code: `{ "code": "PROMO2026" }`
    /// </remarks>
    /// <param name="id">The challenge GUID.</param>
    /// <param name="dto">Submission inputs keyed by question/field ID.</param>
    /// <response code="200">Submission processed. Check `success` field for the result.</response>
    /// <response code="400">Validation error, anti-fraud rejection, or challenge already completed.</response>
    /// <response code="401">Unauthorized — JWT required.</response>
    /// <response code="422">Anti-fraud rule rejection (code not found, already used, campaign inactive).</response>
    /// <response code="429">Rate limit exceeded (IP block, daily limit, bot detection).</response>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SubmitChallenge(Guid id, [FromBody] SubmitChallengeRequestDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var command = new SubmitChallengeCommand(id, userId, dto.Inputs, remoteIp);
        var result = await _sender.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
