using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.InstantWin.Strategies;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Tests.InstantWin;

public class PointsPrizeAwardStrategyTests
{
    private readonly Mock<IApplicationDbContext> _dbMock;
    private readonly Mock<IUserPointMovementRepository> _pointRepoMock;
    private readonly Mock<DbSet<UserPrize>> _userPrizeSetMock;
    private readonly PointsPrizeAwardStrategy _strategy;
    private readonly PrizeSelectionContext _ctx;

    public PointsPrizeAwardStrategyTests()
    {
        _dbMock = new Mock<IApplicationDbContext>();
        _pointRepoMock = new Mock<IUserPointMovementRepository>();
        _userPrizeSetMock = new Mock<DbSet<UserPrize>>();

        _dbMock.Setup(db => db.UserPrizes).Returns(_userPrizeSetMock.Object);
        _dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _dbMock.Setup(db => db.TryDecrementPrizeStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        _strategy = new PointsPrizeAwardStrategy(_dbMock.Object, _pointRepoMock.Object);
        _ctx = new PrizeSelectionContext
        {
            UserId = Guid.NewGuid(),
            ChallengeId = Guid.NewGuid(),
            Now = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public void CanHandle_Points_ReturnsTrue()
    {
        _strategy.CanHandle(PrizeType.Points).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_Product_ReturnsFalse()
    {
        _strategy.CanHandle(PrizeType.Product).Should().BeFalse();
    }

    [Fact]
    public async Task AwardAsync_CreatesUserPrizeWithPointsType()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Name = "100 pts", Quantity = 100, RemainingQuantity = 0, PrizeType = PrizeType.Points };
        UserPrize? captured = null;
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .Callback<UserPrize, CancellationToken>((up, _) => captured = up)
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        var result = await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        result.PrizeType.Should().Be(PrizeType.Points);
        result.UserId.Should().Be(_ctx.UserId);
        result.PrizeId.Should().Be(prize.Id);
    }

    [Fact]
    public async Task AwardAsync_AwardsBasePoints_WhenNoMultiplier()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Quantity = 50, RemainingQuantity = 0, PrizeType = PrizeType.Points };
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        var result = await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);
        result.PointsAwarded.Should().Be(50);
    }

    [Fact]
    public async Task AwardAsync_AppliesPointMultiplier()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Quantity = 100, PointMultiplier = 3, RemainingQuantity = 0, PrizeType = PrizeType.Points };
        _pointRepoMock.Setup(r => r.GetTotalPointsAsync(_ctx.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(200);
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        var result = await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);
        result.PointsAwarded.Should().Be(600); // 3 × 200
    }

    [Fact]
    public async Task AwardAsync_SetsExpiresAt_WhenExpirationDaysConfigured()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Quantity = 100, PointsExpirationDays = 30, RemainingQuantity = 0, PrizeType = PrizeType.Points };
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        var result = await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);
        result.ExpiresAt.Should().Be(_ctx.Now.AddDays(30));
    }

    [Fact]
    public async Task AwardAsync_ExpiresAtIsNull_WhenExpirationDaysIsZero()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Quantity = 100, PointsExpirationDays = 0, RemainingQuantity = 0, PrizeType = PrizeType.Points };
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        var result = await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);
        result.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task AwardAsync_CreatesMovementRecord()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Quantity = 75, RemainingQuantity = 0, PrizeType = PrizeType.Points };
        UserPointMovement? capturedMovement = null;
        _pointRepoMock.Setup(r => r.AddAsync(It.IsAny<UserPointMovement>(), It.IsAny<CancellationToken>()))
            .Callback<UserPointMovement, CancellationToken>((m, _) => capturedMovement = m)
            .Returns(Task.CompletedTask);
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        capturedMovement.Should().NotBeNull();
        capturedMovement!.UserId.Should().Be(_ctx.UserId);
        capturedMovement.Points.Should().Be(75);
        capturedMovement.ChallengeId.Should().Be(_ctx.ChallengeId);
        capturedMovement.PrizeId.Should().Be(prize.Id);
    }

    [Fact]
    public async Task AwardAsync_DecrementsCalled_WhenStockIsLimited()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Quantity = 100, RemainingQuantity = 5, PrizeType = PrizeType.Points };
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        _dbMock.Verify(db => db.TryDecrementPrizeStockAsync(prize.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardAsync_DecrementNotCalled_WhenRemainingQuantityIsZero()
    {
        // RemainingQuantity == 0 means "unlimited" tracking — skip decrement
        var prize = new Prize { Id = Guid.NewGuid(), Quantity = 100, RemainingQuantity = 0, PrizeType = PrizeType.Points };
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        _dbMock.Verify(db => db.TryDecrementPrizeStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AwardAsync_AppliesTicketMultiplier()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Quantity = 10, RemainingQuantity = 0, PrizeType = PrizeType.Points };
        _ctx.TicketLineItemsJson = "[{\"qty\": 3}, {\"qty\": 2}]"; // sum = 5
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        var result = await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);
        result.PointsAwarded.Should().Be(50); // 10 base × 5 ticket qty sum
    }
}
