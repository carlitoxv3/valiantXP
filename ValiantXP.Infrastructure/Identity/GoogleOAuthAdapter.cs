using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Identity;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Infrastructure.Identity;

public class GoogleOAuthAdapter : IOAuthProviderAdapter
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public string ProviderName => "google";

    public GoogleOAuthAdapter(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClient = httpClientFactory.CreateClient("google-oauth");
    }

    public string GetAuthorizationUrl(string redirectUri, string state)
    {
        var clientId = _config["OAuth:Google:ClientId"]
            ?? throw new InvalidOperationException("OAuth:Google:ClientId not configured");

        return $"https://accounts.google.com/o/oauth2/v2/auth" +
               $"?client_id={Uri.EscapeDataString(clientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope=openid%20email%20profile" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&access_type=offline";
    }

    public async Task<ExternalIdentityClaims> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        var clientId = _config["OAuth:Google:ClientId"]
            ?? throw new InvalidOperationException("OAuth:Google:ClientId not configured");
        var clientSecret = _config["OAuth:Google:ClientSecret"]
            ?? throw new InvalidOperationException("OAuth:Google:ClientSecret not configured");

        // Exchange code for tokens
        var tokenResponse = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            }), ct);

        tokenResponse.EnsureSuccessStatusCode();
        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var idToken = tokenJson.GetProperty("id_token").GetString()
            ?? throw new InvalidOperationException("No id_token in Google response");

        // Validate and decode the ID token (Google.Apis.Auth validates signature)
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        });

        return new ExternalIdentityClaims(
            Provider: IdentityProvider.Google,
            ExternalId: payload.Subject,          // the stable 'sub' claim
            EmailClaim: payload.Email,
            IsEmailVerified: payload.EmailVerified,
            ClaimsJson: JsonSerializer.Serialize(new
            {
                sub = payload.Subject,
                email = payload.Email,
                email_verified = payload.EmailVerified,
                name = payload.Name,
                picture = payload.Picture
            })
        );
    }
}
