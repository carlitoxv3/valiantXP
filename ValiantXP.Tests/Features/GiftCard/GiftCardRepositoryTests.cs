using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;
using ValiantXP.Infrastructure.Repositories;

namespace ValiantXP.Tests.Features.GiftCard;

/// <summary>
/// Tests for GiftCardRepository.
/// Note: TryAssignFromPoolAsync uses raw ADO.NET (UPDATE TOP(1)...OUTPUT) which cannot be tested
/// with the InMemory provider. That method is tested via interface mock only.
/// LINQ-based methods (GetAvailableCountAsync, BulkInsertAsync, CodeExistsAsync) use InMemory EF.
/// </summary>
public class GiftCardRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
            .Options;
        return new ApplicationDbContext(options);
    }

    // ── TryAssignFromPoolAsync (interface mock — raw SQL not testable with InMemory) ──

    [Fact]
    public async Task TryAssignFromPoolAsync_WhenPoolEmpty_ReturnsNull()
    {
        // Arrange
        var repoMock = new Mock<IGiftCardRepository>();
        repoMock.Setup(r => r.TryAssignFromPoolAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), default))
            .ReturnsAsync((Domain.Entities.GiftCard?)null);

        // Act
        var result = await repoMock.Object.TryAssignFromPoolAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryAssignFromPoolAsync_WhenPoolHasCard_ReturnsAssignedCard()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userPrizeId = Guid.NewGuid();
        var expectedCard = new Domain.Entities.GiftCard
        {
            Id = Guid.NewGuid(),
            Code = "TEST-CODE-001",
            ProviderId = providerId,
            AssignedToUserId = userId,
            AssignedAt = DateTime.UtcNow
        };

        var repoMock = new Mock<IGiftCardRepository>();
        repoMock.Setup(r => r.TryAssignFromPoolAsync(providerId, userId, userPrizeId, default))
            .ReturnsAsync(expectedCard);

        // Act
        var result = await repoMock.Object.TryAssignFromPoolAsync(providerId, userId, userPrizeId);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("TEST-CODE-001");
        result.ProviderId.Should().Be(providerId);
    }

    // ── GetAvailableCountAsync (real EF InMemory) ──

    [Fact]
    public async Task GetAvailableCountAsync_ReturnsOnlyUnassignedCards()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var providerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await db.GiftCards.AddRangeAsync(new[]
        {
            new Domain.Entities.GiftCard { Id = Guid.NewGuid(), ProviderId = providerId, Code = "CODE-1", AssignedToUserId = null },
            new Domain.Entities.GiftCard { Id = Guid.NewGuid(), ProviderId = providerId, Code = "CODE-2", AssignedToUserId = Guid.NewGuid() }, // assigned
            new Domain.Entities.GiftCard { Id = Guid.NewGuid(), ProviderId = providerId, Code = "CODE-3", AssignedToUserId = null },
            new Domain.Entities.GiftCard { Id = Guid.NewGuid(), ProviderId = otherId,    Code = "CODE-X", AssignedToUserId = null }, // different provider
        });
        await db.SaveChangesAsync();

        var repo = new GiftCardRepository(db);

        // Act
        var count = await repo.GetAvailableCountAsync(providerId);

        // Assert
        count.Should().Be(2, "only 2 cards for this provider are unassigned");
    }

    [Fact]
    public async Task GetAvailableCountAsync_WhenNoCards_ReturnsZero()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var repo = new GiftCardRepository(db);

        // Act
        var count = await repo.GetAvailableCountAsync(Guid.NewGuid());

        // Assert
        count.Should().Be(0);
    }

    // ── BulkInsertAsync (real EF InMemory) ──

    [Fact]
    public async Task BulkInsertAsync_InsertsAllCards()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var providerId = Guid.NewGuid();
        var cards = new List<Domain.Entities.GiftCard>
        {
            new() { Id = Guid.NewGuid(), ProviderId = providerId, Code = "BULK-001" },
            new() { Id = Guid.NewGuid(), ProviderId = providerId, Code = "BULK-002" },
            new() { Id = Guid.NewGuid(), ProviderId = providerId, Code = "BULK-003" },
        };
        var repo = new GiftCardRepository(db);

        // Act
        await repo.BulkInsertAsync(cards);
        await db.SaveChangesAsync();

        // Assert
        var count = await repo.GetAvailableCountAsync(providerId);
        count.Should().Be(3);
    }

    // ── CodeExistsAsync (real EF InMemory) ──

    [Fact]
    public async Task CodeExistsAsync_WhenCodeExists_ReturnsTrue()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var providerId = Guid.NewGuid();
        await db.GiftCards.AddAsync(new Domain.Entities.GiftCard
        {
            Id = Guid.NewGuid(), ProviderId = providerId, Code = "EXISTING-CODE"
        });
        await db.SaveChangesAsync();
        var repo = new GiftCardRepository(db);

        // Act
        var exists = await repo.CodeExistsAsync(providerId, "EXISTING-CODE");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task CodeExistsAsync_WhenCodeDoesNotExist_ReturnsFalse()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var repo = new GiftCardRepository(db);

        // Act
        var exists = await repo.CodeExistsAsync(Guid.NewGuid(), "NONEXISTENT");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task CodeExistsAsync_WhenCodeExistsForDifferentProvider_ReturnsFalse()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var provider1 = Guid.NewGuid();
        var provider2 = Guid.NewGuid();
        await db.GiftCards.AddAsync(new Domain.Entities.GiftCard
        {
            Id = Guid.NewGuid(), ProviderId = provider1, Code = "SHARED-CODE"
        });
        await db.SaveChangesAsync();
        var repo = new GiftCardRepository(db);

        // Act — search same code but under different provider
        var exists = await repo.CodeExistsAsync(provider2, "SHARED-CODE");

        // Assert — code is scoped to provider
        exists.Should().BeFalse();
    }
}
