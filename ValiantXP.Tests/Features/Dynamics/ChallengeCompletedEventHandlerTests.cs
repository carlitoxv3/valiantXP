using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.Features.Dynamics.EventHandlers;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;
using Xunit;

namespace ValiantXP.Tests.Features.Dynamics;

public class ChallengeCompletedEventHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPrizeRepository> _mockPrizeRepo;
    private readonly Mock<IUserPrizeRepository> _mockUserPrizeRepo;
    private readonly ChallengeCompletedEventHandler _handler;

    public ChallengeCompletedEventHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPrizeRepo = new Mock<IPrizeRepository>();
        _mockUserPrizeRepo = new Mock<IUserPrizeRepository>();

        _mockUnitOfWork.Setup(u => u.Prizes).Returns(_mockPrizeRepo.Object);
        _mockUnitOfWork.Setup(u => u.UserPrizes).Returns(_mockUserPrizeRepo.Object);

        _handler = new ChallengeCompletedEventHandler(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithAvailablePrizes_ShouldDecrementStock_RegisterUserPrize_AndGenerateVoucherCode()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var notification = new ChallengeCompletedEvent(userId, challengeId, progressId);

        var prize1 = new Prize
        {
            Id = Guid.NewGuid(),
            DynamicChallengeId = challengeId,
            Name = "Super Coupon",
            Quantity = 10,
            RemainingQuantity = 5,
            Type = "Coupon"
        };

        var prizes = new List<Prize> { prize1 };

        _mockPrizeRepo
            .Setup(r => r.GetByChallengeIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prizes);

        UserPrize? savedUserPrize = null;
        _mockUserPrizeRepo
            .Setup(r => r.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .Callback<UserPrize, CancellationToken>((up, _) => savedUserPrize = up)
            .Returns(Task.CompletedTask);

        _mockPrizeRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Prize>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        prize1.RemainingQuantity.Should().Be(4); // decremented from 5 to 4

        savedUserPrize.Should().NotBeNull();
        savedUserPrize!.UserId.Should().Be(userId);
        savedUserPrize.PrizeId.Should().Be(prize1.Id);
        savedUserPrize.Code.Should().StartWith("VXP-COUPON-");
        savedUserPrize.Code.Length.Should().BeGreaterThan("VXP-COUPON-".Length);
        savedUserPrize.AwardedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));

        _mockPrizeRepo.Verify(r => r.UpdateAsync(prize1, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserPrizeRepo.Verify(r => r.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoRemainingStock_ShouldNotAwardPrizeAndNotDecrementStock()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var notification = new ChallengeCompletedEvent(userId, challengeId, progressId);

        var prize1 = new Prize
        {
            Id = Guid.NewGuid(),
            DynamicChallengeId = challengeId,
            Name = "Super Coupon",
            Quantity = 10,
            RemainingQuantity = 0, // out of stock
            Type = "Coupon"
        };

        var prizes = new List<Prize> { prize1 };

        _mockPrizeRepo
            .Setup(r => r.GetByChallengeIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prizes);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        prize1.RemainingQuantity.Should().Be(0); // stays 0

        _mockPrizeRepo.Verify(r => r.UpdateAsync(It.IsAny<Prize>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUserPrizeRepo.Verify(r => r.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // save changes is still called at the end
    }

    [Fact]
    public async Task Handle_WithMultiplePrizes_OneInStockOneOutOfStock_ShouldOnlyAwardInStockPrize()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();
        var progressId = Guid.NewGuid();
        var notification = new ChallengeCompletedEvent(userId, challengeId, progressId);

        var prizeInStock = new Prize
        {
            Id = Guid.NewGuid(),
            DynamicChallengeId = challengeId,
            Name = "Points Prize",
            Quantity = 100,
            RemainingQuantity = 1,
            Type = "Points"
        };

        var prizeOutOfStock = new Prize
        {
            Id = Guid.NewGuid(),
            DynamicChallengeId = challengeId,
            Name = "Coupon Prize",
            Quantity = 5,
            RemainingQuantity = 0,
            Type = "Coupon"
        };

        var prizes = new List<Prize> { prizeInStock, prizeOutOfStock };

        _mockPrizeRepo
            .Setup(r => r.GetByChallengeIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prizes);

        var addedUserPrizes = new List<UserPrize>();
        _mockUserPrizeRepo
            .Setup(r => r.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()))
            .Callback<UserPrize, CancellationToken>((up, _) => addedUserPrizes.Add(up))
            .Returns(Task.CompletedTask);

        _mockPrizeRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Prize>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        prizeInStock.RemainingQuantity.Should().Be(0);
        prizeOutOfStock.RemainingQuantity.Should().Be(0);

        addedUserPrizes.Should().HaveCount(1);
        addedUserPrizes.First().PrizeId.Should().Be(prizeInStock.Id);
        addedUserPrizes.First().Code.Should().StartWith("VXP-POINTS-");

        _mockPrizeRepo.Verify(r => r.UpdateAsync(prizeInStock, It.IsAny<CancellationToken>()), Times.Once);
        _mockPrizeRepo.Verify(r => r.UpdateAsync(prizeOutOfStock, It.IsAny<CancellationToken>()), Times.Never);
        _mockUserPrizeRepo.Verify(r => r.AddAsync(It.IsAny<UserPrize>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
