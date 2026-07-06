using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ValiantXP.Application.InstantWin.Filters;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Tests.InstantWin;

public class StockFilterTests
{
    private readonly StockFilter _filter = new();
    private readonly PrizeSelectionContext _ctx = new() { UserId = Guid.NewGuid(), ChallengeId = Guid.NewGuid() };

    [Fact]
    public async Task IsEligible_WhenRemainingQuantityIsPositive_ReturnsTrue()
    {
        var prize = new Prize { Id = Guid.NewGuid(), RemainingQuantity = 5 };
        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEligible_WhenRemainingQuantityIsZero_ReturnsFalse()
    {
        var prize = new Prize { Id = Guid.NewGuid(), RemainingQuantity = 0 };
        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_WhenRemainingQuantityIsOne_ReturnsTrue()
    {
        var prize = new Prize { Id = Guid.NewGuid(), RemainingQuantity = 1 };
        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
    }
}
