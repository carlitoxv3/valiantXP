using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ValiantXP.API.Controllers;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;
using ValiantXP.Application.Identity;
using ValiantXP.Domain.Entities;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

public class OAuthControllerTests
{
    // -------------------------------------------------------------------------
    // Shared mocks
    // -------------------------------------------------------------------------
    private readonly Mock<IOAuthProviderAdapter> _googleMock = new();
    private readonly Mock<IOAuthProviderAdapter> _spotifyMock = new();
    private readonly Mock<IIdentityResolutionService> _resolutionMock = new();
    private readonly Mock<ITokenService> _tokenMock = new();
    private readonly OAuthController _sut;

    public OAuthControllerTests()
    {
        _googleMock.Setup(a => a.ProviderName).Returns("google");
        _spotifyMock.Setup(a => a.ProviderName).Returns("spotify");

        _sut = new OAuthController(
            new[] { _googleMock.Object, _spotifyMock.Object },
            _resolutionMock.Object,
            _tokenMock.Object);

        // Provide a minimal HttpContext so Request.Scheme / Request.Host work
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 5001);
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // -------------------------------------------------------------------------
    // GetProviders
    // -------------------------------------------------------------------------

    // O1 — Returns list of all registered provider names
    [Fact]
    public void GetProviders_ReturnsAllRegisteredProviders()
    {
        // Act
        var result = _sut.GetProviders() as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        json.Should().Contain("google");
        json.Should().Contain("spotify");
    }

    // -------------------------------------------------------------------------
    // Initiate
    // -------------------------------------------------------------------------

    // O2 — Unknown provider returns BadRequest
    [Fact]
    public void Initiate_UnknownProvider_ReturnsBadRequest()
    {
        // Act
        var result = _sut.Initiate("nonexistent");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // O3 — Known provider returns Ok with authorization URL
    [Fact]
    public void Initiate_KnownProvider_ReturnsOkWithUrl()
    {
        // Arrange
        _googleMock
            .Setup(a => a.GetAuthorizationUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://accounts.google.com/auth?response_type=code");

        // Act
        var result = _sut.Initiate("google") as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        json.Should().Contain("accounts.google.com");
    }

    // -------------------------------------------------------------------------
    // Callback
    // -------------------------------------------------------------------------

    // O4 — Unknown provider returns BadRequest
    [Fact]
    public async Task Callback_UnknownProvider_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Callback("nonexistent", "code", "state", null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // O5 — Error query param returns BadRequest (user denied OAuth)
    [Fact]
    public async Task Callback_WithError_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.Callback("google", null, null, "access_denied");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // O6 — Missing code returns BadRequest
    [Fact]
    public async Task Callback_MissingCode_ReturnsBadRequest()
    {
        // Act — no error param, but no code either
        var result = await _sut.Callback("google", null, "state-xyz", null);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // O7 — Valid callback resolves identity and returns JWT tokens
    [Fact]
    public async Task Callback_ValidCode_ReturnsResolvedWithTokens()
    {
        // Arrange
        var fakeClaims = new ExternalIdentityClaims(
            Provider: ValiantXP.Domain.Enums.IdentityProvider.Google,
            ExternalId: "google-123",
            EmailClaim: "user@gmail.com",
            IsEmailVerified: true);

        var fakeUser = new User(); // minimal user entity
        var fakeTokens = new TokenResponseDto
        {
            AccessToken = "jwt-token",
            RefreshToken = "refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _googleMock
            .Setup(a => a.ExchangeCodeAsync("valid-code", It.IsAny<string>(), default))
            .ReturnsAsync(fakeClaims);

        _resolutionMock
            .Setup(r => r.ResolveAsync(fakeClaims, default))
            .ReturnsAsync(IdentityResolutionResult.Resolved(fakeUser));

        _tokenMock
            .Setup(t => t.GenerateTokensAsync(fakeUser, It.IsAny<string>()))
            .ReturnsAsync(fakeTokens);

        // Act
        var result = await _sut.Callback("google", "valid-code", "state", null) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        json.Should().Contain("resolved");
        json.Should().Contain("google");
    }

    // O8 — Callback returns linking_required when identity resolution requires linking
    [Fact]
    public async Task Callback_LinkingRequired_ReturnsLinkingRequiredStatus()
    {
        // Arrange
        var fakeClaims = new ExternalIdentityClaims(
            Provider: ValiantXP.Domain.Enums.IdentityProvider.Spotify,
            ExternalId: "spotify-999",
            EmailClaim: "shared@example.com",
            IsEmailVerified: false);

        _spotifyMock
            .Setup(a => a.ExchangeCodeAsync("spotify-code", It.IsAny<string>(), default))
            .ReturnsAsync(fakeClaims);

        _resolutionMock
            .Setup(r => r.ResolveAsync(fakeClaims, default))
            .ReturnsAsync(IdentityResolutionResult.RequiresLinking("link-token-abc", "google"));

        // Act
        var result = await _sut.Callback("spotify", "spotify-code", "state", null) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
        json.Should().Contain("linking_required");
        json.Should().Contain("link-token-abc");
    }
}
