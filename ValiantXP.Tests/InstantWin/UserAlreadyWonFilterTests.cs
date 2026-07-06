using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.InstantWin.Filters;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Tests.InstantWin;

public class UserAlreadyWonFilterTests
{
    private readonly Mock<IUserPrizeRepository> _repoMock = new();
    private readonly UserAlreadyWonFilter _filter;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly PrizeSelectionContext _ctx;

    public UserAlreadyWonFilterTests()
    {
        _filter = new UserAlreadyWonFilter(_repoMock.Object);
        _ctx = new PrizeSelectionContext { UserId = _userId, ChallengeId = Guid.NewGuid() };
    }

    [Fact]
    public async Task IsEligible_WhenPrizeTypeIsPoints_AlwaysReturnsTrue()
    {
        var prize = new Prize { Id = Guid.NewGuid(), PrizeType = PrizeType.Points };
        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
        _repoMock.Verify(r => r.UserAlreadyWonAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsEligible_WhenPrizeTypeIsGiftCard_AlwaysReturnsTrue()
    {
        var prize = new Prize { Id = Guid.NewGuid(), PrizeType = PrizeType.GiftCard };
        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEligible_WhenPrizeTypeIsProduct_AndUserHasNotWon_ReturnsTrue()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, PrizeType = PrizeType.Product };
        _repoMock
            .Setup(r => r.UserAlreadyWonAsync(_userId, prizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEligible_WhenPrizeTypeIsProduct_AndUserAlreadyWon_ReturnsFalse()
    {
        var prizeId = Guid.NewGuid();
        var prize = new Prize { Id = prizeId, PrizeType = PrizeType.Product };
        _repoMock
            .Setup(r => r.UserAlreadyWonAsync(_userId, prizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _filter.IsEligibleAsync(prize, _ctx, CancellationToken.None);
        result.Should().BeFalse();
    }
}
