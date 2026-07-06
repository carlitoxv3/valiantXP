using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
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

        return Ok(new
        {
            user.Id,
            user.Email,
            user.UserName,
            user.PhoneNumber,
            user.IsMfaEnabled,
            user.CreatedAt
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
}
