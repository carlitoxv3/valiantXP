using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ValiantXP.Domain.Enums;
using ValiantXP.Infrastructure.Identity;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

public class TwitchOAuthAdapterTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static IConfiguration BuildConfig(
        string? clientId = "twitch-client-id",
        string? clientSecret = "twitch-client-secret")
    {
        var data = new Dictionary<string, string?>();
        if (clientId is not null) data["OAuth:Twitch:ClientId"] = clientId;
        if (clientSecret is not null) data["OAuth:Twitch:ClientSecret"] = clientSecret;
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static IHttpClientFactory BuildFactory(params (HttpStatusCode status, string body)[] responses)
    {
        var queue = new Queue<(HttpStatusCode, string)>(responses);
        var handler = new TwitchSequentialFakeHandler(queue);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    /// <summary>
    /// Creates a minimal unsigned JWT (ReadJwtToken does not validate signature).
    /// </summary>
    private static string CreateFakeIdToken(
        string subject,
        string email,
        bool emailVerified,
        string preferredUsername)
    {
        var handler = new JwtSecurityTokenHandler();
        var claims = new[]
        {
            new Claim("email", email),
            new Claim("email_verified", emailVerified.ToString().ToLower()),
            new Claim("preferred_username", preferredUsername)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims, null, ClaimTypes.NameIdentifier, null),
            Issuer = "https://id.twitch.tv/oauth2",
            Expires = DateTime.UtcNow.AddHours(1),
            // No signing credentials — ReadJwtToken skips signature validation
        };
        // We need to set Subject claim manually (maps to 'sub')
        var jwt = new JwtSecurityToken(
            issuer: "https://id.twitch.tv/oauth2",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, subject),
                new Claim("email", email),
                new Claim("email_verified", emailVerified.ToString().ToLower()),
                new Claim("preferred_username", preferredUsername)
            },
            expires: DateTime.UtcNow.AddHours(1)
        );
        return handler.WriteToken(jwt);
    }

    // -------------------------------------------------------------------------
    // GetAuthorizationUrl tests
    // -------------------------------------------------------------------------

    // T1 — URL targets Twitch and includes required scopes
    [Fact]
    public void GetAuthorizationUrl_WithValidConfig_ReturnsTwitchUrl()
    {
        // Arrange
        var adapter = new TwitchOAuthAdapter(BuildConfig(), BuildFactory());

        // Act
        var url = adapter.GetAuthorizationUrl("https://app.example.com/callback", "state-abc");

        // Assert
        url.Should().Contain("id.twitch.tv");
        url.Should().Contain("client_id=twitch-client-id");
        url.Should().Contain("openid");
        url.Should().Contain("user%3Aread%3Aemail");
    }

    // T2 — Missing ClientId throws InvalidOperationException
    [Fact]
    public void GetAuthorizationUrl_MissingClientId_ThrowsInvalidOperationException()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var adapter = new TwitchOAuthAdapter(emptyConfig, BuildFactory());

        // Act & Assert
        var act = () => adapter.GetAuthorizationUrl("https://app.example.com/callback", "state");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OAuth:Twitch:ClientId*");
    }

    // T3 — ProviderName returns "twitch"
    [Fact]
    public void ProviderName_ReturnsTwitch()
    {
        var adapter = new TwitchOAuthAdapter(BuildConfig(), BuildFactory());
        adapter.ProviderName.Should().Be("twitch");
    }

    // -------------------------------------------------------------------------
    // ExchangeCodeAsync tests
    // -------------------------------------------------------------------------

    // T4 — When id_token present, extracts email and email_verified from JWT
    [Fact]
    public async Task ExchangeCodeAsync_WithIdToken_ExtractsEmailVerified()
    {
        // Arrange
        var idToken = CreateFakeIdToken(
            subject: "twitch_user_123",
            email: "user@twitch.tv",
            emailVerified: true,
            preferredUsername: "twitchuser");

        var tokenJson =
            $"{{\"access_token\":\"acc-tok\",\"token_type\":\"bearer\"," +
            $"\"id_token\":\"{idToken}\"}}";

        var adapter = new TwitchOAuthAdapter(
            BuildConfig(),
            BuildFactory((HttpStatusCode.OK, tokenJson)));

        // Act
        var claims = await adapter.ExchangeCodeAsync("auth-code", "https://app.example.com/callback");

        // Assert
        claims.Provider.Should().Be(IdentityProvider.Twitch);
        claims.IsEmailVerified.Should().BeTrue();
        claims.EmailClaim.Should().Be("user@twitch.tv");
        claims.ExternalId.Should().Be("twitch_user_123");
    }

    // T5 — When no id_token, falls back to Helix API and retrieves userId
    [Fact]
    public async Task ExchangeCodeAsync_WithoutIdToken_FallsBackToHelixApi()
    {
        // Arrange — token response has NO id_token
        const string tokenJson = "{\"access_token\":\"acc-tok\",\"token_type\":\"bearer\"}";
        const string helixJson =
            "{\"data\":[{\"id\":\"helix-user-456\",\"login\":\"helixuser\",\"email\":\"helix@twitch.tv\"}]}";

        var adapter = new TwitchOAuthAdapter(
            BuildConfig(),
            BuildFactory(
                (HttpStatusCode.OK, tokenJson),   // token endpoint
                (HttpStatusCode.OK, helixJson)));  // helix endpoint

        // Act
        var claims = await adapter.ExchangeCodeAsync("auth-code", "https://app.example.com/callback");

        // Assert
        claims.Provider.Should().Be(IdentityProvider.Twitch);
        claims.ExternalId.Should().Be("helix-user-456");
        claims.EmailClaim.Should().Be("helix@twitch.tv");
        // Helix doesn't return email_verified → stays false
        claims.IsEmailVerified.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // Inner helper
    // -------------------------------------------------------------------------

    private sealed class TwitchSequentialFakeHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _queue;

        public TwitchSequentialFakeHandler(Queue<(HttpStatusCode, string)> queue)
            => _queue = queue;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_queue.Count == 0)
                throw new InvalidOperationException("No more fake HTTP responses queued.");

            var (status, body) = _queue.Dequeue();
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
