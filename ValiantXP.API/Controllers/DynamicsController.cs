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

[Authorize]
[ApiController]
[Route("api/dynamics")]
public class DynamicsController : ControllerBase
{
    private readonly ISender _sender;

    public DynamicsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id:guid}")]
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

    [HttpPost("{id:guid}/submit")]
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
