using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Identity;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Infrastructure.Identity;

/// <summary>
/// Twitch OAuth 2.0 + OIDC adapter.
/// Scope: openid user:read:email
/// Provides email_verified via OIDC ID token claims.
/// ExternalId = Twitch numeric user ID (stable, does not change).
/// </summary>
public class TwitchOAuthAdapter : IOAuthProviderAdapter
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderName => "twitch";

    public TwitchOAuthAdapter(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public string GetAuthorizationUrl(string redirectUri, string state)
    {
        var clientId = _config["OAuth:Twitch:ClientId"]
            ?? throw new InvalidOperationException("OAuth:Twitch:ClientId not configured");

        return "https://id.twitch.tv/oauth2/authorize" +
               $"?client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope=openid%20user%3Aread%3Aemail" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&claims={Uri.EscapeDataString("{\"id_token\":{\"email\":null,\"email_verified\":null}}")}";
    }

    public async Task<ExternalIdentityClaims> ExchangeCodeAsync(
        string code, string redirectUri, CancellationToken ct = default)
    {
        var clientId = _config["OAuth:Twitch:ClientId"]
            ?? throw new InvalidOperationException("OAuth:Twitch:ClientId not configured");
        var clientSecret = _config["OAuth:Twitch:ClientSecret"]
            ?? throw new InvalidOperationException("OAuth:Twitch:ClientSecret not configured");

        var httpClient = _httpClientFactory.CreateClient("twitch-oauth");

        // Step 1: Exchange code for tokens
        var tokenResponse = await httpClient.PostAsync(
            "https://id.twitch.tv/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            }), ct);

        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        var accessToken = tokenJson.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("No access_token in Twitch response");

        // Step 2: Extract claims from OIDC id_token if available
        string? twitchUserId = null;
        string? email = null;
        bool emailVerified = false;
        string? preferredUsername = null;

        if (tokenJson.TryGetProperty("id_token", out var idTokenProp))
        {
            var idTokenStr = idTokenProp.GetString();
            if (!string.IsNullOrEmpty(idTokenStr))
            {
                // Decode JWT without validation (Twitch OIDC — we trust the exchange endpoint)
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(idTokenStr);
                twitchUserId = jwt.Subject; // 'sub' = Twitch numeric user ID
                email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                emailVerified = jwt.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value == "true";
                preferredUsername = jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;
            }
        }

        // Step 3: Fallback — use Helix API if OIDC id_token didn't have user info
        if (string.IsNullOrEmpty(twitchUserId))
        {
            var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
            userRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            userRequest.Headers.Add("Client-Id", clientId);
            var userResponse = await httpClient.SendAsync(userRequest, ct);
            userResponse.EnsureSuccessStatusCode();
            var userJson = await userResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var userData = userJson.GetProperty("data")[0];
            twitchUserId = userData.GetProperty("id").GetString();
            email = userData.TryGetProperty("email", out var ep) ? ep.GetString() : null;
            preferredUsername = userData.TryGetProperty("login", out var lp) ? lp.GetString() : null;
            // Helix API doesn't return email_verified — keep false
        }

        return new ExternalIdentityClaims(
            Provider: IdentityProvider.Twitch,
            ExternalId: twitchUserId ?? throw new InvalidOperationException("Could not determine Twitch user ID"),
            EmailClaim: email,
            IsEmailVerified: emailVerified,
            ClaimsJson: JsonSerializer.Serialize(new
            {
                sub = twitchUserId,
                email,
                email_verified = emailVerified,
                preferred_username = preferredUsername
            })
        );
    }
}
