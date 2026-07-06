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
using ValiantXP.Domain.Interfaces;
using ValiantXP.Domain.Exceptions;

namespace ValiantXP.Tests.InstantWin;

public class ProductPrizeAwardStrategyTests
{
    private readonly Mock<IApplicationDbContext> _dbMock;
    private readonly Mock<DbSet<UserPrize>> _userPrizeSetMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IGiftCardRepository> _giftCardRepoMock;
    private readonly ProductPrizeAwardStrategy _strategy;
    private readonly PrizeSelectionContext _ctx;

    public ProductPrizeAwardStrategyTests()
    {
        _dbMock = new Mock<IApplicationDbContext>();
        _userPrizeSetMock = new Mock<DbSet<UserPrize>>();
        _uowMock = new Mock<IUnitOfWork>();
        _giftCardRepoMock = new Mock<IGiftCardRepository>();

        _dbMock.Setup(db => db.UserPrizes).Returns(_userPrizeSetMock.Object);
        _dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _dbMock.Setup(db => db.TryDecrementPrizeStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        // GiftCard pool: by default no GiftCardProviderId is set on prizes in these tests,
        // so TryAssignFromPoolAsync will not be called. Set up as returning null just in case.
        _uowMock.Setup(u => u.GiftCards).Returns(_giftCardRepoMock.Object);
        _giftCardRepoMock.Setup(r => r.TryAssignFromPoolAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GiftCard?)null);

        _strategy = new ProductPrizeAwardStrategy(_dbMock.Object, _uowMock.Object);
        _ctx = new PrizeSelectionContext
        {
            UserId = Guid.NewGuid(),
            ChallengeId = Guid.NewGuid(),
            Now = DateTime.UtcNow
        };
    }

    [Fact]
    public void CanHandle_Product_ReturnsTrue()
    {
        _strategy.CanHandle(PrizeType.Product).Should().BeTrue();
    }

    [Fact]
    public void CanHandle_Points_ReturnsFalse()
    {
        _strategy.CanHandle(PrizeType.Points).Should().BeFalse();
    }

    [Fact]
    public async Task AwardAsync_CreatesUserPrizeWithProductType()
    {
        var prize = new Prize { Id = Guid.NewGuid(), Name = "Laptop", PrizeType = PrizeType.Product };
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        var result = await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        result.PrizeType.Should().Be(PrizeType.Product);
        result.UserId.Should().Be(_ctx.UserId);
        result.PrizeId.Should().Be(prize.Id);
        result.Code.Should().StartWith("VXP-PROD-");
    }

    [Fact]
    public async Task AwardAsync_ThrowsWhenStockDepleted()
    {
        _dbMock.Setup(db => db.TryDecrementPrizeStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(false); // stock was 0 — race condition

        var prize = new Prize { Id = Guid.NewGuid(), Name = "TV", PrizeType = PrizeType.Product };

        Func<Task> act = () => _strategy.AwardAsync(prize, _ctx, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*stock depleted*");
    }

    [Fact]
    public async Task AwardAsync_CallsDecrementBeforeSave()
    {
        var callOrder = new System.Collections.Generic.List<string>();

        _dbMock.Setup(db => db.TryDecrementPrizeStockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .Callback(() => callOrder.Add("decrement"))
               .ReturnsAsync(true);
        _dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .Callback(() => callOrder.Add("save"))
               .ReturnsAsync(1);
        _userPrizeSetMock.Setup(s => s.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserPrize>)null!);

        var prize = new Prize { Id = Guid.NewGuid(), PrizeType = PrizeType.Product };
        await _strategy.AwardAsync(prize, _ctx, CancellationToken.None);

        callOrder[0].Should().Be("decrement", "stock must be decremented before saving the award");
        callOrder[1].Should().Be("save");
    }
}
