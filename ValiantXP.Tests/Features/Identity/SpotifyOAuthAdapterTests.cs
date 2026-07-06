using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ValiantXP.Domain.Enums;
using ValiantXP.Infrastructure.Identity;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

public class SpotifyOAuthAdapterTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static IConfiguration BuildConfig(
        string? clientId = "spotify-client-id",
        string? clientSecret = "spotify-client-secret")
    {
        var data = new Dictionary<string, string?>();
        if (clientId is not null) data["OAuth:Spotify:ClientId"] = clientId;
        if (clientSecret is not null) data["OAuth:Spotify:ClientSecret"] = clientSecret;
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    /// <summary>
    /// Creates a factory that returns an HttpClient backed by a handler that
    /// alternates responses according to the supplied sequence.
    /// </summary>
    private static IHttpClientFactory BuildFactory(params (HttpStatusCode status, string body)[] responses)
    {
        var queue = new Queue<(HttpStatusCode, string)>(responses);
        var handler = new SequentialFakeHandler(queue);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    // -------------------------------------------------------------------------
    // GetAuthorizationUrl tests
    // -------------------------------------------------------------------------

    // C1 — URL targets Spotify, includes client_id and expected scope
    [Fact]
    public void GetAuthorizationUrl_WithValidConfig_ReturnsSpotifyUrl()
    {
        // Arrange
        var adapter = new SpotifyOAuthAdapter(BuildConfig(), BuildFactory());

        // Act
        var url = adapter.GetAuthorizationUrl("https://app.example.com/callback", "state-xyz");

        // Assert
        url.Should().Contain("accounts.spotify.com");
        url.Should().Contain("client_id=spotify-client-id");
        url.Should().Contain("user-read-email");
    }

    // C2 — Missing ClientId throws InvalidOperationException
    [Fact]
    public void GetAuthorizationUrl_MissingClientId_ThrowsInvalidOperationException()
    {
        // Arrange — config has no ClientId key at all
        var emptyConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var adapter = new SpotifyOAuthAdapter(emptyConfig, BuildFactory());

        // Act & Assert
        var act = () => adapter.GetAuthorizationUrl("https://app.example.com/callback", "state");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OAuth:Spotify:ClientId*");
    }

    // -------------------------------------------------------------------------
    // ExchangeCodeAsync tests
    // -------------------------------------------------------------------------

    // C3 — IsEmailVerified is always false (Spotify design decision)
    [Fact]
    public async Task ExchangeCodeAsync_ValidResponse_ReturnsClaimsWithIsEmailVerifiedFalse()
    {
        // Arrange
        const string tokenJson = "{\"access_token\":\"tok-abc\",\"token_type\":\"Bearer\",\"expires_in\":3600}";
        const string profileJson = "{\"id\":\"spotify-user-1\",\"email\":\"user@spotify.com\",\"display_name\":\"Test User\"}";

        var adapter = new SpotifyOAuthAdapter(
            BuildConfig(),
            BuildFactory(
                (HttpStatusCode.OK, tokenJson),
                (HttpStatusCode.OK, profileJson)));

        // Act
        var claims = await adapter.ExchangeCodeAsync("auth-code", "https://app.example.com/callback");

        // Assert
        claims.IsEmailVerified.Should().BeFalse("Spotify does not expose email_verified");
        claims.Provider.Should().Be(IdentityProvider.Spotify);
    }

    // C4 — Email from profile is mapped to EmailClaim
    [Fact]
    public async Task ExchangeCodeAsync_ProfileHasEmail_SetsEmailClaim()
    {
        // Arrange
        const string tokenJson = "{\"access_token\":\"tok-abc\",\"token_type\":\"Bearer\",\"expires_in\":3600}";
        const string profileJson = "{\"id\":\"spotify-user-2\",\"email\":\"hello@music.com\",\"display_name\":\"Music Fan\"}";

        var adapter = new SpotifyOAuthAdapter(
            BuildConfig(),
            BuildFactory(
                (HttpStatusCode.OK, tokenJson),
                (HttpStatusCode.OK, profileJson)));

        // Act
        var claims = await adapter.ExchangeCodeAsync("code", "https://app.example.com/callback");

        // Assert
        claims.EmailClaim.Should().Be("hello@music.com");
        claims.ExternalId.Should().Be("spotify-user-2");
    }

    // C5 — ProviderName returns "spotify"
    [Fact]
    public void ProviderName_ReturnsSpotify()
    {
        var adapter = new SpotifyOAuthAdapter(BuildConfig(), BuildFactory());
        adapter.ProviderName.Should().Be("spotify");
    }

    // -------------------------------------------------------------------------
    // Inner helper — sequential fake HTTP handler
    // -------------------------------------------------------------------------

    private sealed class SequentialFakeHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _queue;

        public SequentialFakeHandler(Queue<(HttpStatusCode, string)> queue)
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
