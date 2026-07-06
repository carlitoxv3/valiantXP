using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.InstantWin.Filters;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Tests.InstantWin;

public class GlobalWindowFilterTests
{
    private readonly Mock<IUserPrizeRepository> _repoMock = new();
    private readonly GlobalWindowFilter _filter;
    private readonly PrizeSelectionContext _ctx;

    public GlobalWindowFilterTests()
    {
        _filter = new GlobalWindowFilter(_repoMock.Object);
        _ctx = new PrizeSelectionContext
        {
            UserId = Guid.NewGuid(),
            ChallengeId = Guid.NewGuid(),
            Now = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public async Task IsEligible_WhenMaxGlobalInWindowIsZero_AlwaysReturnsTrue()
    {
        var prize = new Prize { Id = Guid.NewGuid(), MaxGlobalInWindow = 0, WindowHours = 24 };
        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
        _repoMock.Verify(r => r.GetAwardCountInWindowAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsEligible_WhenWindowHoursIsZero_AlwaysReturnsTrue()
    {
        var prize = new Prize { Id = Guid.NewGuid(), MaxGlobalInWindow = 5, WindowHours = 0 };
        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
        _repoMock.Verify(r => r.GetAwardCountInWindowAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsEligible_WhenAwardCountBelowLimit_ReturnsTrue()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, MaxGlobalInWindow = 10, WindowHours = 24 };
        _repoMock
            .Setup(r => r.GetAwardCountInWindowAsync(prizeId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEligible_WhenAwardCountAtLimit_ReturnsFalse()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, MaxGlobalInWindow = 10, WindowHours = 24 };
        _repoMock
            .Setup(r => r.GetAwardCountInWindowAsync(prizeId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_WhenAwardCountAboveLimit_ReturnsFalse()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, MaxGlobalInWindow = 10, WindowHours = 24 };
        _repoMock
            .Setup(r => r.GetAwardCountInWindowAsync(prizeId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEligible_PassesCorrectWindowStart()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, MaxGlobalInWindow = 5, WindowHours = 48 };
        DateTime capturedWindowStart = default;
        _repoMock
            .Setup(r => r.GetAwardCountInWindowAsync(prizeId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DateTime, CancellationToken>((_, ws, _) => capturedWindowStart = ws)
            .ReturnsAsync(0);

        await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);

        var expectedWindowStart = _ctx.Now.AddHours(-48);
        capturedWindowStart.Should().Be(expectedWindowStart);
    }
}
