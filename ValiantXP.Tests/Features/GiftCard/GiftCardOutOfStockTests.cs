using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.InstantWin.Strategies;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Tests.Features.GiftCard;

/// <summary>
/// Tests for ProductPrizeAwardStrategy GiftCard-delivery scenarios:
///   1. prize.GiftCardProviderId == null → normal flow (no pool lookup)
///   2. prize.GiftCardProviderId != null + pool has code → assigns GiftCardCode
///   3. prize.GiftCardProviderId != null + pool empty → throws GiftCardOutOfStockException
/// </summary>
public class GiftCardOutOfStockTests
{
    private readonly Mock<IApplicationDbContext> _dbMock;
    private readonly Mock<DbSet<UserPrize>> _userPrizeSetMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IGiftCardRepository> _giftCardRepoMock;
    private readonly PrizeSelectionContext _ctx;

    public GiftCardOutOfStockTests()
    {
        _dbMock = new Mock<IApplicationDbContext>();
        _userPrizeSetMock = new Mock<DbSet<UserPrize>>();
        _uowMock = new Mock<IUnitOfWork>();
        _giftCardRepoMock = new Mock<IGiftCardRepository>();

        _dbMock.Setup(db => db.UserPrizes).Returns(_userPrizeSetMock.Object);
        _dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _dbMock.Setup(db => db.TryDecrementPrizeStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        _uowMock.Setup(u => u.GiftCards).Returns(_giftCardRepoMock.Object);

        _ctx = new PrizeSelectionContext
        {
            UserId = Guid.NewGuid(),
            ChallengeId = Guid.NewGuid(),
            Now = DateTime.UtcNow
        };
    }

    private ProductPrizeAwardStrategy BuildStrategy() =>
        new ProductPrizeAwardStrategy(_dbMock.Object, _uowMock.Object);

    // ── Scenario 1: No GiftCard provider — normal product flow ──

    [Fact]
    public async Task AwardAsync_WhenNoGiftCardProviderId_DoesNotCallPool_AndSetsInternalCode()
    {
        // Arrange — prize has no GiftCardProviderId (physical product)
        var prize = new Prize
        {
            Id = Guid.NewGuid(),
            Name = "Headphones",
            PrizeType = PrizeType.Product,
            GiftCardProviderId = null
        };
        var strategy = BuildStrategy();

        // Act
        var result = await strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        // Assert — pool never consulted for physical products
        _giftCardRepoMock.Verify(r => r.TryAssignFromPoolAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        result.GiftCardCode.Should().BeNull("no pool assignment for physical product");
        result.Code.Should().StartWith("VXP-PROD-", "internal code is generated for non-GiftCard products");
        result.PrizeType.Should().Be(PrizeType.Product);
    }

    // ── Scenario 2: GiftCard provider + pool has available code → assigns GiftCardCode ──

    [Fact]
    public async Task AwardAsync_WhenGiftCardProviderSet_AndPoolHasCode_AssignsGiftCardCode()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var expectedCode = "AMAZON-GIFT-ABC123";
        var prize = new Prize
        {
            Id = Guid.NewGuid(),
            Name = "Amazon Gift Card €50",
            PrizeType = PrizeType.Product,
            GiftCardProviderId = providerId
        };

        _giftCardRepoMock.Setup(r => r.TryAssignFromPoolAsync(
                providerId, _ctx.UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Entities.GiftCard
            {
                Id = Guid.NewGuid(),
                Code = expectedCode,
                ProviderId = providerId
            });

        var strategy = BuildStrategy();

        // Act
        var result = await strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        // Assert
        result.GiftCardCode.Should().Be(expectedCode, "the assigned pool code must be stored on UserPrize");
        result.PrizeType.Should().Be(PrizeType.Product);
        result.UserId.Should().Be(_ctx.UserId);

        _giftCardRepoMock.Verify(r => r.TryAssignFromPoolAsync(
            providerId, _ctx.UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AwardAsync_WhenGiftCardProviderSet_AndPoolHasCode_SavesUserPrize()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var prize = new Prize
        {
            Id = Guid.NewGuid(),
            Name = "Sodexo €25",
            PrizeType = PrizeType.Product,
            GiftCardProviderId = providerId
        };

        _giftCardRepoMock.Setup(r => r.TryAssignFromPoolAsync(
                providerId, _ctx.UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Entities.GiftCard { Id = Guid.NewGuid(), Code = "SODEXO-001", ProviderId = providerId });

        var strategy = BuildStrategy();

        // Act
        await strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        // Assert — DB must be saved after the award
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Scenario 3: GiftCard provider + pool empty → throws GiftCardOutOfStockException ──

    [Fact]
    public async Task AwardAsync_WhenGiftCardProviderSet_AndPoolEmpty_ThrowsGiftCardOutOfStockException()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var prize = new Prize
        {
            Id = Guid.NewGuid(),
            Name = "Netflix €10",
            PrizeType = PrizeType.Product,
            GiftCardProviderId = providerId
        };

        // Pool is empty — TryAssignFromPool returns null
        _giftCardRepoMock.Setup(r => r.TryAssignFromPoolAsync(
                providerId, _ctx.UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.GiftCard?)null);

        var strategy = BuildStrategy();

        // Act
        Func<Task> act = () => strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<GiftCardOutOfStockException>();
        ex.Which.ProviderId.Should().Be(providerId, "exception must carry the provider ID for diagnostics");
    }

    [Fact]
    public async Task AwardAsync_WhenGiftCardOutOfStock_DoesNotSaveUserPrize()
    {
        // Arrange — pool is empty
        var providerId = Guid.NewGuid();
        var prize = new Prize
        {
            Id = Guid.NewGuid(),
            Name = "Spotify €10",
            PrizeType = PrizeType.Product,
            GiftCardProviderId = providerId
        };

        _giftCardRepoMock.Setup(r => r.TryAssignFromPoolAsync(
                providerId, _ctx.UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.GiftCard?)null);

        var strategy = BuildStrategy();

        // Act
        Func<Task> act = () => strategy.AwardAsync(prize, _ctx, CancellationToken.None);
        await act.Should().ThrowAsync<GiftCardOutOfStockException>();

        // Assert — no UserPrize persisted when pool is empty
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never,
            "SaveChanges must NOT be called when GiftCard pool is out of stock");
    }

    // ── Stock decrement still happens before pool assignment ──

    [Fact]
    public async Task AwardAsync_WithGiftCardProvider_StillDecrementsStockFirst()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var callOrder = new System.Collections.Generic.List<string>();

        _dbMock.Setup(db => db.TryDecrementPrizeStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .Callback(() => callOrder.Add("decrement"))
               .ReturnsAsync(true);

        _giftCardRepoMock.Setup(r => r.TryAssignFromPoolAsync(
                providerId, _ctx.UserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("pool"))
            .ReturnsAsync(new Domain.Entities.GiftCard { Id = Guid.NewGuid(), Code = "GC-ORDER-TEST", ProviderId = providerId });

        var prize = new Prize { Id = Guid.NewGuid(), PrizeType = PrizeType.Product, GiftCardProviderId = providerId };
        var strategy = BuildStrategy();

        // Act
        await strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        // Assert — stock decremented before pool assignment
        callOrder[0].Should().Be("decrement", "stock must be decremented atomically before any pool operation");
        callOrder[1].Should().Be("pool");
    }
}
