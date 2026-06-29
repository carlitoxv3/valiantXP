using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _unitOfWork.Users.GetAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found." });
        }

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
}
