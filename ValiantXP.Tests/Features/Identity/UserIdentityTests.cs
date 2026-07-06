using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Infrastructure.Data;
using ValiantXP.Infrastructure.Repositories;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

public class UserIdentityTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserIdentityRepository _userIdentityRepo;
    private readonly GuestSessionRepository _guestSessionRepo;

    public UserIdentityTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _userIdentityRepo = new UserIdentityRepository(_context);
        _guestSessionRepo = new GuestSessionRepository(_context);
    }

    private User CreateUser(string email = "test@example.com")
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
        _context.SaveChanges();
        return user;
    }

    // A13.1
    [Fact]
    public async Task UserIdentityRepository_FindAsync_ReturnsMatch()
    {
        // Arrange
        var user = CreateUser();
        var identity = new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = IdentityProvider.Google,
            ExternalId = "google-sub-123",
            IsActive = true,
            LinkedAt = DateTime.UtcNow
        };
        await _userIdentityRepo.AddAsync(identity);
        await _context.SaveChangesAsync();

        // Act
        var found = await _userIdentityRepo.FindAsync(IdentityProvider.Google, "google-sub-123");

        // Assert
        found.Should().NotBeNull();
        found!.ExternalId.Should().Be("google-sub-123");
        found.Provider.Should().Be(IdentityProvider.Google);
    }

    // A13.2
    [Fact]
    public async Task UserIdentityRepository_FindAsync_ReturnsNull_WhenInactive()
    {
        // Arrange
        var user = CreateUser();
        var identity = new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = IdentityProvider.Google,
            ExternalId = "google-inactive-456",
            IsActive = false,  // Soft-unlinked
            LinkedAt = DateTime.UtcNow
        };
        await _userIdentityRepo.AddAsync(identity);
        await _context.SaveChangesAsync();

        // Act
        var found = await _userIdentityRepo.FindAsync(IdentityProvider.Google, "google-inactive-456");

        // Assert
        found.Should().BeNull("inactive identities must not be returned");
    }

    // A13.3
    [Fact]
    public async Task UserIdentityRepository_FindByEmailClaim_OnlyVerified()
    {
        // Arrange
        var user1 = CreateUser("verified@example.com");
        var user2 = CreateUser("verified2@example.com");

        await _userIdentityRepo.AddAsync(new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            Provider = IdentityProvider.Google,
            ExternalId = "g1",
            EmailClaim = "shared@example.com",
            IsEmailVerified = true,
            IsActive = true,
            LinkedAt = DateTime.UtcNow
        });
        await _userIdentityRepo.AddAsync(new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            Provider = IdentityProvider.Spotify,
            ExternalId = "s1",
            EmailClaim = "shared@example.com",
            IsEmailVerified = false, // unverified — should be filtered
            IsActive = true,
            LinkedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var results = await _userIdentityRepo.FindByEmailClaimAsync("shared@example.com", onlyVerified: true);

        // Assert
        results.Should().HaveCount(1);
        results[0].IsEmailVerified.Should().BeTrue();
        results[0].Provider.Should().Be(IdentityProvider.Google);
    }

    // A13.4
    [Fact]
    public async Task UserIdentityRepository_CountActive_ExcludesUnlinked()
    {
        // Arrange
        var user = CreateUser();
        await _userIdentityRepo.AddAsync(new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = IdentityProvider.Google,
            ExternalId = "active-1",
            IsActive = true,
            LinkedAt = DateTime.UtcNow
        });
        await _userIdentityRepo.AddAsync(new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = IdentityProvider.Spotify,
            ExternalId = "inactive-1",
            IsActive = false, // soft-deleted
            LinkedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var count = await _userIdentityRepo.CountActiveByUserAsync(user.Id);

        // Assert
        count.Should().Be(1, "only the active identity should be counted");
    }

    // A13.5
    [Fact]
    public void GuestSession_IsExpired_ReturnsTrue()
    {
        // Arrange
        var session = new GuestSession
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // already expired
        };

        // Assert
        session.IsExpired.Should().BeTrue();
        session.IsConverted.Should().BeFalse();
    }

    // A13.6
    [Fact]
    public void GuestSession_IsConverted_AfterConversion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = new GuestSession
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            ConvertedToUserId = userId,
            ConvertedAt = DateTime.UtcNow
        };

        // Assert
        session.IsConverted.Should().BeTrue();
        session.IsExpired.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
