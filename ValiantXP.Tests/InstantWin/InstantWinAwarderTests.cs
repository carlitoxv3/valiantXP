using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.InstantWin;
using ValiantXP.Application.InstantWin.Strategies;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Tests.InstantWin;

public class InstantWinAwarderTests
{
    private readonly PrizeSelectionContext _ctx = new()
    {
        UserId = Guid.NewGuid(),
        ChallengeId = Guid.NewGuid(),
        Now = DateTime.UtcNow
    };

    private static Mock<IPrizeAwardStrategy> CreateStrategyMock(PrizeType handles)
    {
        var mock = new Mock<IPrizeAwardStrategy>();
        mock.Setup(s => s.CanHandle(handles)).Returns(true);
        mock.Setup(s => s.CanHandle(It.Is<PrizeType>(t => t != handles))).Returns(false);
        mock.Setup(s => s.AwardAsync(It.IsAny<Prize>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPrize { Id = Guid.NewGuid(), PrizeType = handles });
        return mock;
    }

    [Fact]
    public async Task AwardAsync_RoutesToPointsStrategy()
    {
        var pointsMock = CreateStrategyMock(PrizeType.Points);
        var awarder = new InstantWinAwarder(new[] { pointsMock.Object });
        var prize = new Prize { PrizeType = PrizeType.Points };

        var result = await awarder.AwardAsync(prize, _ctx, CancellationToken.None);

        result.PrizeType.Should().Be(PrizeType.Points);
        pointsMock.Verify(s => s.AwardAsync(prize, _ctx, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AwardAsync_RoutesToProductStrategy()
    {
        var productMock = CreateStrategyMock(PrizeType.Product);
        var awarder = new InstantWinAwarder(new[] { productMock.Object });
        var prize = new Prize { PrizeType = PrizeType.Product };

        var result = await awarder.AwardAsync(prize, _ctx, CancellationToken.None);

        result.PrizeType.Should().Be(PrizeType.Product);
        productMock.Verify(s => s.AwardAsync(prize, _ctx, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // PrizeType.GiftCard was deprecated (Sprint 10 GC-5).
    // GiftCard delivery is now PrizeType.Product + Prize.GiftCardProviderId != null.
    // The awarder routes by PrizeType — Product strategy handles both physical products and GiftCard-delivered products.
    public async Task AwardAsync_RoutesToProductStrategy_ForGiftCardDeliveredPrize()
    {
        var productMock = CreateStrategyMock(PrizeType.Product);
        var awarder = new InstantWinAwarder(new[] { productMock.Object });
        // GiftCard-delivered prize: PrizeType = Product, GiftCardProviderId set by caller
        var prize = new Prize { PrizeType = PrizeType.Product, GiftCardProviderId = Guid.NewGuid() };

        var result = await awarder.AwardAsync(prize, _ctx, CancellationToken.None);

        result.PrizeType.Should().Be(PrizeType.Product);
    }

    [Fact]
    public async Task AwardAsync_ThrowsNotSupported_WhenNoStrategyFound()
    {
        // No strategies registered
        var awarder = new InstantWinAwarder(Array.Empty<IPrizeAwardStrategy>());
        var prize = new Prize { PrizeType = PrizeType.Points };

        Func<Task> act = () => awarder.AwardAsync(prize, _ctx, CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Points*");
    }

    [Fact]
    public async Task AwardAsync_ReturnsUserPrize_OnSuccess()
    {
        var pointsMock = CreateStrategyMock(PrizeType.Points);
        var expectedId = Guid.NewGuid();
        pointsMock.Setup(s => s.AwardAsync(It.IsAny<Prize>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPrize { Id = expectedId, PrizeType = PrizeType.Points });

        var awarder = new InstantWinAwarder(new[] { pointsMock.Object });
        var prize = new Prize { PrizeType = PrizeType.Points };

        var result = await awarder.AwardAsync(prize, _ctx, CancellationToken.None);
        result.Id.Should().Be(expectedId);
    }
}
