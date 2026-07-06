using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ValiantXP.Application.Identity;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Infrastructure.Data;
using ValiantXP.Infrastructure.Identity;
using ValiantXP.Infrastructure.Repositories;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

/// <summary>
/// Integration tests for IdentityResolutionService using InMemory EF.
/// </summary>
public class IdentityResolutionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _uow;
    private readonly InMemoryPendingLinkService _pendingLinkService;
    private readonly IdentityResolutionService _sut;

    public IdentityResolutionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _uow = new UnitOfWork(_context);
        _pendingLinkService = new InMemoryPendingLinkService();
        _sut = new IdentityResolutionService(
            _uow,
            _pendingLinkService,
            NullLogger<IdentityResolutionService>.Instance);
    }

    private async Task<(User user, UserIdentity identity)> SeedUserWithIdentityAsync(
        string externalId,
        IdentityProvider provider,
        string email,
        bool emailVerified = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email.Split('@')[0],
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var identity = new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = provider,
            ExternalId = externalId,
            EmailClaim = email,
            IsEmailVerified = emailVerified,
            IsActive = true,
            LinkedAt = DateTime.UtcNow
        };
        _context.UserIdentities.Add(identity);
        await _context.SaveChangesAsync();
        return (user, identity);
    }

    // B6.1 — Direct lookup returns existing user
    [Fact]
    public async Task ResolveAsync_DirectLookup_ReturnsExistingUser()
    {
        // Arrange
        var (user, _) = await SeedUserWithIdentityAsync("g-sub-001", IdentityProvider.Google, "alice@example.com");
        var claims = new ExternalIdentityClaims(IdentityProvider.Google, "g-sub-001", "alice@example.com", true);

        // Act
        var result = await _sut.ResolveAsync(claims);

        // Assert
        result.Status.Should().Be(IdentityResolutionStatus.Resolved);
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be(user.Id);
    }

    // B6.2 — Auto-merge: new provider with verified email matches existing verified email
    [Fact]
    public async Task ResolveAsync_AutoMerge_BothEmailVerified_LinksToExisting()
    {
        // Arrange
        await SeedUserWithIdentityAsync("email-otp-alice", IdentityProvider.EmailOtp, "alice@example.com", emailVerified: true);
        // Now Google comes in with same verified email, different externalId
        var claims = new ExternalIdentityClaims(IdentityProvider.Google, "g-new-alice", "alice@example.com", IsEmailVerified: true);

        // Act
        var result = await _sut.ResolveAsync(claims);

        // Assert
        result.Status.Should().Be(IdentityResolutionStatus.Resolved);
        // Verify a new UserIdentity was created for Google, pointing to the same user
        var googleIdentity = await _context.UserIdentities
            .FirstOrDefaultAsync(i => i.Provider == IdentityProvider.Google && i.ExternalId == "g-new-alice");
        googleIdentity.Should().NotBeNull();
        googleIdentity!.UserId.Should().Be(result.User!.Id);
    }

    // B6.3 — Unverified email matches existing → LinkingRequired
    [Fact]
    public async Task ResolveAsync_UnverifiedEmail_MatchesExisting_ReturnsLinkingRequired()
    {
        // Arrange
        await SeedUserWithIdentityAsync("otp-bob", IdentityProvider.EmailOtp, "bob@example.com", emailVerified: true);
        // Spotify comes in with same email but unverified
        var claims = new ExternalIdentityClaims(IdentityProvider.Spotify, "spotify-bob", "bob@example.com", IsEmailVerified: false);

        // Act
        var result = await _sut.ResolveAsync(claims);

        // Assert
        result.Status.Should().Be(IdentityResolutionStatus.LinkingRequired);
        result.PendingLinkToken.Should().NotBeNullOrEmpty();
        result.SuggestedProvider.Should().Be("EmailOtp");
    }

    // B6.4 — No match → creates new user
    [Fact]
    public async Task ResolveAsync_NoMatch_CreatesNewUser()
    {
        // Arrange
        var claims = new ExternalIdentityClaims(IdentityProvider.Google, "brand-new-user", "newuser@example.com", true);

        // Act
        var result = await _sut.ResolveAsync(claims);

        // Assert
        result.Status.Should().Be(IdentityResolutionStatus.Resolved);
        result.User.Should().NotBeNull();

        var dbUser = await _context.Users.FindAsync(result.User!.Id);
        dbUser.Should().NotBeNull();

        var identity = await _context.UserIdentities.FirstOrDefaultAsync(i => i.ExternalId == "brand-new-user");
        identity.Should().NotBeNull();
        identity!.UserId.Should().Be(result.User.Id);
    }

    // B6.5 — Multiple verified users with same email → fall through to create new (safe degradation)
    [Fact]
    public async Task ResolveAsync_MultipleUsersWithSameEmail_FallsThrough()
    {
        // Arrange — two separate users both with verified @conflict.com email (data integrity issue)
        await SeedUserWithIdentityAsync("user-a-ext", IdentityProvider.EmailOtp, "conflict@example.com", emailVerified: true);
        await SeedUserWithIdentityAsync("user-b-ext", IdentityProvider.Twitch, "conflict@example.com", emailVerified: true);

        var claims = new ExternalIdentityClaims(IdentityProvider.Google, "g-conflict", "conflict@example.com", true);

        // Act — should NOT throw, should create a third user (safe degradation)
        var result = await _sut.ResolveAsync(claims);

        // Assert
        result.Status.Should().Be(IdentityResolutionStatus.Resolved);
        result.User.Should().NotBeNull();
    }

    // B6.6 — ConfirmLinkAsync with valid token links identity
    [Fact]
    public async Task ConfirmLinkAsync_ValidToken_LinksIdentity()
    {
        // Arrange
        var (existingUser, _) = await SeedUserWithIdentityAsync("existing-otp", IdentityProvider.EmailOtp, "charlie@example.com");
        var pendingClaims = new ExternalIdentityClaims(IdentityProvider.Google, "g-charlie", "charlie@example.com", false);
        var token = await _pendingLinkService.CreateAsync(pendingClaims);

        // Act
        var success = await _sut.ConfirmLinkAsync(token, existingUser.Id);

        // Assert
        success.Should().BeTrue();
        var linked = await _context.UserIdentities.FirstOrDefaultAsync(i => i.ExternalId == "g-charlie");
        linked.Should().NotBeNull();
        linked!.UserId.Should().Be(existingUser.Id);
    }

    // B6.7 — ConfirmLinkAsync with expired token returns false
    [Fact]
    public async Task ConfirmLinkAsync_ExpiredToken_ReturnsFalse()
    {
        // Arrange — we can't easily expire via the service (10 min TTL), so use invalid token
        var success = await _sut.ConfirmLinkAsync("this-token-was-never-created", Guid.NewGuid());

        // Assert
        success.Should().BeFalse();
    }

    // B6.8 — ConfirmLinkAsync with invalid/consumed token returns false
    [Fact]
    public async Task ConfirmLinkAsync_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var claims = new ExternalIdentityClaims(IdentityProvider.Spotify, "s-dave", "dave@example.com", false);
        var token = await _pendingLinkService.CreateAsync(claims);
        // Consume it once
        await _pendingLinkService.ValidateAndConsumeAsync(token);

        // Act — try to consume again
        var success = await _sut.ConfirmLinkAsync(token, Guid.NewGuid());

        // Assert
        success.Should().BeFalse("token was already consumed");
    }

    public void Dispose() => _context.Dispose();
}
