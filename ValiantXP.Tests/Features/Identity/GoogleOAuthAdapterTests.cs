using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ValiantXP.Application.Identity;
using ValiantXP.Infrastructure.Identity;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

public class GoogleOAuthAdapterTests
{
    private readonly IConfiguration _configWithClientId;

    public GoogleOAuthAdapterTests()
    {
        var configData = new System.Collections.Generic.Dictionary<string, string?>
        {
            ["OAuth:Google:ClientId"] = "test-client-id-12345",
            ["OAuth:Google:ClientSecret"] = "test-client-secret"
        };
        _configWithClientId = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private GoogleOAuthAdapter CreateAdapter(IHttpClientFactory? factory = null)
    {
        factory ??= CreateMockHttpClientFactory();
        return new GoogleOAuthAdapter(_configWithClientId, factory);
    }

    private static IHttpClientFactory CreateMockHttpClientFactory()
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new System.Net.Http.HttpClient());
        return mockFactory.Object;
    }

    // C5.1 — Authorization URL contains client_id
    [Fact]
    public void GetAuthorizationUrl_ContainsClientId()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act
        var url = adapter.GetAuthorizationUrl("https://app.example.com/callback", "state123");

        // Assert
        url.Should().Contain("client_id=test-client-id-12345");
        url.Should().Contain("accounts.google.com");
        url.Should().Contain("response_type=code");
    }

    // C5.2 — Authorization URL contains redirect_uri
    [Fact]
    public void GetAuthorizationUrl_ContainsRedirectUri()
    {
        // Arrange
        var adapter = CreateAdapter();

        // Act
        var url = adapter.GetAuthorizationUrl("https://app.example.com/callback", "state-abc");

        // Assert
        url.Should().Contain(Uri.EscapeDataString("https://app.example.com/callback"));
        url.Should().Contain("state=state-abc");
    }

    // C5.3 — ExchangeCodeAsync throws when token validation fails (invalid token)
    [Fact]
    public async Task ExchangeCodeAsync_InvalidToken_ThrowsException()
    {
        // Arrange — we mock the HTTP client to return a response with a fake id_token
        // GoogleJsonWebSignature.ValidateAsync will throw since the token is invalid
        var mockHandler = new MockHttpMessageHandler(System.Net.HttpStatusCode.OK,
            "{\"id_token\":\"invalid.jwt.token\",\"access_token\":\"fake\"}");

        var httpClient = new System.Net.Http.HttpClient(mockHandler);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var adapter = new GoogleOAuthAdapter(_configWithClientId, mockFactory.Object);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            () => adapter.ExchangeCodeAsync("fake-code", "https://app.example.com/callback"));
    }
}

/// <summary>Minimal HttpMessageHandler stub for unit testing HTTP clients.</summary>
public class MockHttpMessageHandler : System.Net.Http.HttpMessageHandler
{
    private readonly System.Net.HttpStatusCode _statusCode;
    private readonly string _content;

    public MockHttpMessageHandler(System.Net.HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
        System.Net.Http.HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new System.Net.Http.HttpResponseMessage(_statusCode)
        {
            Content = new System.Net.Http.StringContent(_content, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
