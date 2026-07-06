using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Infrastructure.Data;
using ValiantXP.Infrastructure.Repositories;
using Xunit;

namespace ValiantXP.Tests.Features.Identity;

public class IdentityLinkingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserIdentityRepository _repo;

    public IdentityLinkingTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _repo = new UserIdentityRepository(_context);
    }

    private async Task<User> SeedUserAsync(string email = "user@example.com")
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
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<UserIdentity> SeedIdentityAsync(Guid userId, IdentityProvider provider, string externalId, bool isActive = true)
    {
        var identity = new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ExternalId = externalId,
            IsActive = isActive,
            LinkedAt = DateTime.UtcNow
        };
        await _repo.AddAsync(identity);
        await _context.SaveChangesAsync();
        return identity;
    }

    // E2.1 — GetByUserAsync returns linked providers for a user
    [Fact]
    public async Task GetMyIdentities_ReturnsLinkedProviders()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedIdentityAsync(user.Id, IdentityProvider.Google, "g-001");
        await SeedIdentityAsync(user.Id, IdentityProvider.Spotify, "s-001");

        // Act
        var identities = await _repo.GetByUserAsync(user.Id);

        // Assert
        identities.Should().HaveCount(2);
        identities.Should().Contain(i => i.Provider == IdentityProvider.Google);
        identities.Should().Contain(i => i.Provider == IdentityProvider.Spotify);
    }

    // E2.2 — Soft-unlinking reduces active count; identity is preserved with IsActive=false
    [Fact]
    public async Task UnlinkIdentity_Success_WhenMultipleIdentities()
    {
        // Arrange
        var user = await SeedUserAsync();
        var id1 = await SeedIdentityAsync(user.Id, IdentityProvider.Google, "g-002");
        await SeedIdentityAsync(user.Id, IdentityProvider.EmailOtp, "e-002");

        // Act — simulate soft delete
        var activeCountBefore = await _repo.CountActiveByUserAsync(user.Id);
        id1.IsActive = false;
        id1.UnlinkedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(id1);
        await _context.SaveChangesAsync();
        var activeCountAfter = await _repo.CountActiveByUserAsync(user.Id);

        // Assert
        activeCountBefore.Should().Be(2);
        activeCountAfter.Should().Be(1);

        // Soft-deleted record is still in DB for audit
        var inDb = await _context.UserIdentities.FindAsync(id1.Id);
        inDb.Should().NotBeNull();
        inDb!.IsActive.Should().BeFalse();
        inDb.UnlinkedAt.Should().NotBeNull();
    }

    // E2.3 — CountActive = 1 means the only identity must not be unlinked (business rule enforced by controller)
    [Fact]
    public async Task UnlinkIdentity_Blocked_WhenLastIdentity()
    {
        // Arrange
        var user = await SeedUserAsync();
        await SeedIdentityAsync(user.Id, IdentityProvider.Google, "g-only");

        // Act
        var count = await _repo.CountActiveByUserAsync(user.Id);

        // Assert — controller uses this count to block unlinking
        count.Should().Be(1, "a single identity cannot be unlinked");
    }

    // E2.4 — GetByUserAsync does NOT return other users' identities
    [Fact]
    public async Task UnlinkIdentity_NotFound_WhenNotOwned()
    {
        // Arrange
        var user1 = await SeedUserAsync("user1@example.com");
        var user2 = await SeedUserAsync("user2@example.com");
        await SeedIdentityAsync(user1.Id, IdentityProvider.Google, "g-user1");
        var id2 = await SeedIdentityAsync(user2.Id, IdentityProvider.Spotify, "s-user2");

        // Act — query user1's identities and try to find user2's identity
        var user1Identities = await _repo.GetByUserAsync(user1.Id);
        var targetBelongingToOtherUser = user1Identities.FirstOrDefault(i => i.Id == id2.Id);

        // Assert — not found in user1's list (mimics controller 404 behavior)
        targetBelongingToOtherUser.Should().BeNull("user1 cannot see user2's identities");
    }

    public void Dispose() => _context.Dispose();
}
