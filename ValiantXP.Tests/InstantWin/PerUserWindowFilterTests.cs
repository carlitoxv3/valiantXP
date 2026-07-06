using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.InstantWin.Filters;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Tests.InstantWin;

public class PerUserWindowFilterTests
{
    private readonly Mock<IUserPrizeRepository> _repoMock = new();
    private readonly PerUserWindowFilter _filter;
    private readonly PrizeSelectionContext _ctx;
    private readonly Guid _userId = Guid.NewGuid();

    public PerUserWindowFilterTests()
    {
        _filter = new PerUserWindowFilter(_repoMock.Object);
        _ctx = new PrizeSelectionContext
        {
            UserId = _userId,
            ChallengeId = Guid.NewGuid(),
            Now = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public async Task IsEligible_WhenMaxPerUserInWindowIsZero_AlwaysReturnsTrue()
    {
        var prize = new Prize { Id = Guid.NewGuid(), MaxPerUserInWindow = 0, WindowHours = 24 };
        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
        _repoMock.Verify(r => r.GetUserAwardCountInWindowAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsEligible_WhenUserCountBelowLimit_ReturnsTrue()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, MaxPerUserInWindow = 3, WindowHours = 24 };
        _repoMock
            .Setup(r => r.GetUserAwardCountInWindowAsync(_userId, prizeId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEligible_WhenUserCountAtLimit_ReturnsFalse()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, MaxPerUserInWindow = 3, WindowHours = 24 };
        _repoMock
            .Setup(r => r.GetUserAwardCountInWindowAsync(_userId, prizeId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_OtherUsersAwardsDoNotCount()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, MaxPerUserInWindow = 1, WindowHours = 24 };

        // Our user has 0 awards, but mock must match user ID specifically
        _repoMock
            .Setup(r => r.GetUserAwardCountInWindowAsync(_userId, prizeId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // our user's count

        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
    }
}
