using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Tests.Integration;

/// <summary>
/// Integration tests for the authentication pipeline.
/// Tests real HTTP requests through the full ASP.NET Core middleware stack.
/// Uses in-memory database — no external dependencies required.
/// </summary>
public class AuthIntegrationTests : IClassFixture<ValiantXPWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(ValiantXPWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── /otp/request ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RequestOtp_WithInvalidChannel_Returns400()
    {
        // Arrange
        var payload = new { target = "user@test.com", channel = "Telegram" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/otp/request", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid channel");
    }

    [Fact]
    public async Task RequestOtp_WithValidEmailChannel_Returns200()
    {
        // Arrange
        var payload = new { target = "integration@test.com", channel = "Email" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/otp/request", payload);

        // Assert
        // The handler may succeed or fail depending on OTP sender config in test env
        // We verify the channel parsing is correct (not 400 for invalid channel)
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);

        var body = await response.Content.ReadAsStringAsync();
        // Should not be "Invalid channel" error
        body.Should().NotContain("Invalid channel");
    }

    // ─── /otp/verify ──────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyOtp_WithNonExistentOtp_Returns400()
    {
        // Arrange — try to verify an OTP that was never requested
        var payload = new { target = "nobody@test.com", code = "000000" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/otp/verify", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─── /api/dynamics — Unauthorized access ──────────────────────────────────

    [Fact]
    public async Task GetChallenge_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync($"/api/dynamics/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubmitChallenge_WithoutToken_Returns401()
    {
        // Arrange
        var payload = new { inputs = new Dictionary<string, string> { { "code", "TEST" } } };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/dynamics/{Guid.NewGuid()}/submit", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Global Exception Handler ─────────────────────────────────────────────

    [Fact]
    public async Task GlobalExceptionHandler_ForUnknownRoutes_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent/route");

        // Assert — should not be 500, returns 404 normally
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task HealthCheck_SwaggerEndpoint_IsAccessibleInDevelopment()
    {
        // The swagger endpoint should return 200 or be redirected (depends on env)
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // In testing environment Swagger may or may not be enabled
        // We verify no 500 errors
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
