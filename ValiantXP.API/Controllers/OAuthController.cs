using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.Identity;

namespace ValiantXP.API.Controllers;

/// <summary>
/// Provider-agnostic OAuth 2.0 social login flows.
/// Supported providers: google, spotify, twitch (extensible via IOAuthProviderAdapter).
/// </summary>
[ApiController]
[Route("api/auth/oauth")]
[Produces("application/json")]
public class OAuthController : ControllerBase
{
    private readonly IEnumerable<IOAuthProviderAdapter> _adapters;
    private readonly IIdentityResolutionService _resolutionService;
    private readonly ITokenService _tokenService;

    public OAuthController(
        IEnumerable<IOAuthProviderAdapter> adapters,
        IIdentityResolutionService resolutionService,
        ITokenService tokenService)
    {
        _adapters = adapters;
        _resolutionService = resolutionService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// List all available OAuth providers.
    /// Use the returned provider names as the {provider} parameter in other endpoints.
    /// </summary>
    /// <returns>Array of objects with provider name.</returns>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public IActionResult GetProviders()
        => Ok(_adapters.Select(a => new { provider = a.ProviderName }));

    /// <summary>
    /// Initiate OAuth login for the specified provider.
    /// Returns the authorization URL and state to redirect the user.
    /// </summary>
    /// <param name="provider">Provider name: google, spotify, twitch</param>
    /// <returns>Authorization URL, state token, and provider name.</returns>
    [HttpGet("{provider}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public IActionResult Initiate(string provider)
    {
        var adapter = GetAdapter(provider);
        if (adapter is null)
            return BadRequest(new
            {
                error = $"Unknown provider '{provider}'. Available: {string.Join(", ", _adapters.Select(a => a.ProviderName))}"
            });

        var redirectUri = GetRedirectUri(provider);
        var state = Guid.NewGuid().ToString("N");
        // In production: store state in session/cache to validate on callback
        var authUrl = adapter.GetAuthorizationUrl(redirectUri, state);
        return Ok(new { url = authUrl, state, provider });
    }

    /// <summary>
    /// OAuth callback — exchanges authorization code, resolves identity, issues JWT.
    /// </summary>
    /// <param name="provider">Provider name: google, spotify, twitch</param>
    /// <param name="code">Authorization code returned by the provider.</param>
    /// <param name="state">State token returned by the provider (CSRF protection).</param>
    /// <param name="error">Error code if the user denied authorization.</param>
    /// <returns>JWT token pair on success, or linking_required status when an account merge is needed.</returns>
    [HttpGet("{provider}/callback")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Callback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error)
    {
        // Handle user denial
        if (!string.IsNullOrEmpty(error))
            return BadRequest(new { error = $"OAuth denied: {error}" });

        if (string.IsNullOrEmpty(code))
            return BadRequest(new { error = "Authorization code is required" });

        var adapter = GetAdapter(provider);
        if (adapter is null)
            return BadRequest(new { error = $"Unknown provider '{provider}'" });

        var redirectUri = GetRedirectUri(provider);
        var claims = await adapter.ExchangeCodeAsync(code, redirectUri);
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
        return Ok(new { status = "resolved", provider, tokens });
    }

    /// <summary>
    /// Confirm a pending identity link (D1 non-auto merge case).
    /// Called when the user confirms linking an unverified-email provider to an existing account.
    /// </summary>
    /// <param name="request">Request body containing the pending link token.</param>
    /// <returns>Success message or error if token is invalid/expired.</returns>
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

    // -------------------------------------------------------------------------
    private IOAuthProviderAdapter? GetAdapter(string provider)
        => _adapters.FirstOrDefault(a => a.ProviderName == provider.ToLower());

    private string GetRedirectUri(string provider)
        => $"{Request.Scheme}://{Request.Host}/api/auth/oauth/{provider}/callback";
}

public record ConfirmLinkRequest(string PendingLinkToken);
