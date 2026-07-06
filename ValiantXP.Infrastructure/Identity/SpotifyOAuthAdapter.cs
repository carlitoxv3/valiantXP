using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Identity;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Infrastructure.Identity;

/// <summary>
/// Spotify OAuth 2.0 adapter.
/// Scope: user-read-email user-read-private
/// Note: Spotify does NOT provide email_verified claim.
/// IsEmailVerified is always false → triggers Step 3 (prompt user) in IdentityResolutionService.
/// </summary>
public class SpotifyOAuthAdapter : IOAuthProviderAdapter
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderName => "spotify";

    public SpotifyOAuthAdapter(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public string GetAuthorizationUrl(string redirectUri, string state)
    {
        var clientId = _config["OAuth:Spotify:ClientId"]
            ?? throw new InvalidOperationException("OAuth:Spotify:ClientId not configured");

        return "https://accounts.spotify.com/authorize" +
               $"?client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope=user-read-email%20user-read-private" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&show_dialog=false";
    }

    public async Task<ExternalIdentityClaims> ExchangeCodeAsync(
        string code, string redirectUri, CancellationToken ct = default)
    {
        var clientId = _config["OAuth:Spotify:ClientId"]
            ?? throw new InvalidOperationException("OAuth:Spotify:ClientId not configured");
        var clientSecret = _config["OAuth:Spotify:ClientSecret"]
            ?? throw new InvalidOperationException("OAuth:Spotify:ClientSecret not configured");

        var httpClient = _httpClientFactory.CreateClient("spotify-oauth");

        // Step 1: Exchange code for access token
        var basicAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        });

        var tokenResponse = await httpClient.SendAsync(tokenRequest, ct);
        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var accessToken = tokenJson.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("No access_token in Spotify response");

        // Step 2: Get user profile
        var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var profileResponse = await httpClient.SendAsync(profileRequest, ct);
        profileResponse.EnsureSuccessStatusCode();
        var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        var spotifyId = profile.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("No id in Spotify profile");

        string? email = null;
        if (profile.TryGetProperty("email", out var emailProp))
            email = emailProp.GetString();

        string? displayName = null;
        if (profile.TryGetProperty("display_name", out var nameProp))
            displayName = nameProp.GetString();

        return new ExternalIdentityClaims(
            Provider: IdentityProvider.Spotify,
            ExternalId: spotifyId,
            EmailClaim: email,
            // Spotify does NOT expose email_verified — treat as unverified
            IsEmailVerified: false,
            ClaimsJson: JsonSerializer.Serialize(new
            {
                id = spotifyId,
                email,
                display_name = displayName,
                email_verified = false // explicit — Spotify doesn't verify
            })
        );
    }
}
