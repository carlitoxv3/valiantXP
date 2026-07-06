using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.InstantWin;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Tests.InstantWin;

public class InstantWinEngineTests
{
    private readonly PrizeSelectionContext _ctx = new()
    {
        UserId = Guid.NewGuid(),
        ChallengeId = Guid.NewGuid(),
        Now = DateTime.UtcNow
    };

    private static InstantWinEngine CreateEngine(params IPrizeFilter[] filters)
        => new InstantWinEngine(filters);

    private static IPrizeFilter AlwaysPass()
    {
        var mock = new Mock<IPrizeFilter>();
        mock.Setup(f => f.IsEligibleAsync(It.IsAny<Prize>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return mock.Object;
    }

    private static IPrizeFilter AlwaysFail()
    {
        var mock = new Mock<IPrizeFilter>();
        mock.Setup(f => f.IsEligibleAsync(It.IsAny<Prize>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return mock.Object;
    }

    [Fact]
    public async Task TrySelectPrize_WhenNoPrizes_ReturnsNull()
    {
        var engine = CreateEngine(AlwaysPass());
        var result = await engine.TrySelectPrizeAsync(new List<Prize>(), _ctx, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task TrySelectPrize_WhenPrizePasses_ReturnsPrize()
    {
        var prize = new Prize { Id = Guid.NewGuid(), RemainingQuantity = 5 };
        var engine = CreateEngine(AlwaysPass());
        var result = await engine.TrySelectPrizeAsync(new List<Prize> { prize }, _ctx, CancellationToken.None);
        result.Should().Be(prize);
    }

    [Fact]
    public async Task TrySelectPrize_WhenAllFiltersBlock_ReturnsNull()
    {
        var prize = new Prize { Id = Guid.NewGuid(), RemainingQuantity = 0 };
        var engine = CreateEngine(AlwaysFail());
        var result = await engine.TrySelectPrizeAsync(new List<Prize> { prize }, _ctx, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task TrySelectPrize_WhenAllowNoWinTrue_MayReturnNull()
    {
        var prize = new Prize { Id = Guid.NewGuid(), RemainingQuantity = 5, AllowNoWin = true };
        var engine = CreateEngine(AlwaysPass());

        // Run 200 times — with AllowNoWin=true (2-slot pool: prize + null), statistically null should occur
        bool gotNull = false;
        for (int i = 0; i < 200; i++)
        {
            var result = await engine.TrySelectPrizeAsync(new List<Prize> { prize }, _ctx, CancellationToken.None);
            if (result is null)
            {
                gotNull = true;
                break;
            }
        }
        gotNull.Should().BeTrue("with AllowNoWin=true, null should be selectable");
    }

    [Fact]
    public async Task TrySelectPrize_WhenAllowNoWinFalse_NeverReturnsNull()
    {
        var prize = new Prize { Id = Guid.NewGuid(), RemainingQuantity = 5, AllowNoWin = false };
        var engine = CreateEngine(AlwaysPass());

        for (int i = 0; i < 50; i++)
        {
            var result = await engine.TrySelectPrizeAsync(new List<Prize> { prize }, _ctx, CancellationToken.None);
            result.Should().NotBeNull("AllowNoWin=false means always win when stock is available");
        }
    }

    [Fact]
    public async Task TrySelectPrize_WithMultiplePrizes_SelectsFromEligible()
    {
        var prize1 = new Prize { Id = Guid.NewGuid(), Name = "Prize1", RemainingQuantity = 5 };
        var prize2 = new Prize { Id = Guid.NewGuid(), Name = "Prize2", RemainingQuantity = 5 };

        // First filter blocks prize1 only
        var filter = new Mock<IPrizeFilter>();
        filter.Setup(f => f.IsEligibleAsync(prize1, It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        filter.Setup(f => f.IsEligibleAsync(prize2, It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        var engine = CreateEngine(filter.Object);
        var result = await engine.TrySelectPrizeAsync(new List<Prize> { prize1, prize2 }, _ctx, CancellationToken.None);
        result.Should().Be(prize2);
    }

    [Fact]
    public async Task TrySelectPrize_SelectionIsRandom_BothPrizesSelectedOverManyRuns()
    {
        var prize1 = new Prize { Id = Guid.NewGuid(), Name = "Prize1", RemainingQuantity = 5 };
        var prize2 = new Prize { Id = Guid.NewGuid(), Name = "Prize2", RemainingQuantity = 5 };
        var engine = CreateEngine(AlwaysPass());

        var selected = new HashSet<Guid>();
        for (int i = 0; i < 100; i++)
        {
            var result = await engine.TrySelectPrizeAsync(new List<Prize> { prize1, prize2 }, _ctx, CancellationToken.None);
            if (result != null) selected.Add(result.Id);
        }

        selected.Should().Contain(prize1.Id, "random selection should pick prize1 at least once in 100 runs");
        selected.Should().Contain(prize2.Id, "random selection should pick prize2 at least once in 100 runs");
    }
}
