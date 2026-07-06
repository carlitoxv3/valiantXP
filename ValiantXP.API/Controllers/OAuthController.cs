using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.Identity;

namespace ValiantXP.API.Controllers;

/// <summary>OAuth 2.0 social login flows.</summary>
[ApiController]
[Route("api/auth/oauth")]
[Produces("application/json")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthProviderAdapter _googleAdapter;
    private readonly IIdentityResolutionService _resolutionService;
    private readonly ITokenService _tokenService;

    public OAuthController(
        IOAuthProviderAdapter googleAdapter,
        IIdentityResolutionService resolutionService,
        ITokenService tokenService)
    {
        _googleAdapter = googleAdapter;
        _resolutionService = resolutionService;
        _tokenService = tokenService;
    }

    /// <summary>Initiate Google OAuth flow — returns authorization URL.</summary>
    [HttpGet("google")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult InitiateGoogle()
    {
        var redirectUri = GetRedirectUri("google");
        var state = Guid.NewGuid().ToString("N");
        // In production: store state in session/cache to validate on callback
        var authUrl = _googleAdapter.GetAuthorizationUrl(redirectUri, state);
        return Ok(new { url = authUrl, state });
    }

    /// <summary>Google OAuth callback — exchanges code for JWT.</summary>
    [HttpGet("google/callback")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code))
            return BadRequest(new { error = "Authorization code is required" });

        var redirectUri = GetRedirectUri("google");
        var claims = await _googleAdapter.ExchangeCodeAsync(code, redirectUri);
        var result = await _resolutionService.ResolveAsync(claims);

        if (result.Status == IdentityResolutionStatus.LinkingRequired)
        {
            return Ok(new
            {
                status = "linking_required",
                pendingLinkToken = result.PendingLinkToken,
                suggestedProvider = result.SuggestedProvider,
                message = "An account with this email already exists. Please confirm to link."
            });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var tokens = await _tokenService.GenerateTokensAsync(result.User!, ip);
        return Ok(new { status = "resolved", tokens });
    }

    /// <summary>Confirm linking of a pending identity (D1 non-auto merge).</summary>
    [HttpPost("link/confirm")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ConfirmLink([FromBody] ConfirmLinkRequest request)
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (userIdValue is null || !Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var success = await _resolutionService.ConfirmLinkAsync(request.PendingLinkToken, userId);
        if (!success)
            return BadRequest(new { error = "Invalid or expired link token" });

        return Ok(new { message = "Identity linked successfully" });
    }

    private string GetRedirectUri(string provider)
        => $"{Request.Scheme}://{Request.Host}/api/auth/oauth/{provider}/callback";
}

public record ConfirmLinkRequest(string PendingLinkToken);
