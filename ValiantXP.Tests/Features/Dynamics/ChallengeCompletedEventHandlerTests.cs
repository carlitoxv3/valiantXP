using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ValiantXP.Application.Features.Dynamics.EventHandlers;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;
using Xunit;

namespace ValiantXP.Tests.Features.Dynamics;

public class ChallengeCompletedEventHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IPrizeRepository> _mockPrizeRepo = new();
    private readonly Mock<IDynamicChallengeRepository> _mockChallengeRepo = new();
    private readonly Mock<IInstantWinEngine> _mockEngine = new();
    private readonly Mock<IInstantWinAwarder> _mockAwarder = new();
    private readonly Mock<MediatR.IPublisher> _mockPublisher = new();
    private readonly Mock<IUserChallengeProgressRepository> _mockProgressRepo = new();
    private readonly Mock<IUserPointMovementRepository> _mockPointRepo = new();
    private readonly ChallengeCompletedEventHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _challengeId = Guid.NewGuid();
    private readonly Guid _progressId = Guid.NewGuid();

    public ChallengeCompletedEventHandlerTests()
    {
        _mockUnitOfWork.Setup(u => u.Prizes).Returns(_mockPrizeRepo.Object);
        _mockUnitOfWork.Setup(u => u.DynamicChallenges).Returns(_mockChallengeRepo.Object);
        // Default: GetAsync returns a challenge; progressRepo returns null reservation
        // so the position_based branch is skipped and normal InstantWin runs.
        _mockChallengeRepo
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DynamicChallenge { Id = _challengeId, CampaignId = Guid.NewGuid(), ConfigurationJson = "{}" });
        _mockProgressRepo
            .Setup(r => r.GetLatestCodeProgressWithReservationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserChallengeProgress?)null);
        _handler = new ChallengeCompletedEventHandler(
            _mockUnitOfWork.Object,
            _mockEngine.Object,
            _mockAwarder.Object,
            _mockPublisher.Object,
            NullLogger<ChallengeCompletedEventHandler>.Instance,
            _mockProgressRepo.Object,
            _mockPointRepo.Object);
    }

    private ChallengeCompletedEvent MakeEvent(Guid? submissionId = null)
        => new(_userId, _challengeId, _progressId, submissionId);

    private Prize MakePrize(int remaining = 5, PrizeType type = PrizeType.Points)
        => new() { Id = Guid.NewGuid(), Name = "Test Prize", RemainingQuantity = remaining, PrizeType = type, Quantity = 100 };

    private UserPrize MakeUserPrize(Prize prize) =>
        new() { Id = Guid.NewGuid(), UserId = _userId, PrizeId = prize.Id, PrizeType = prize.PrizeType, PointsAwarded = 100 };

    // --- Scenario 1: No prizes configured ---
    [Fact]
    public async Task Handle_WhenNoPrizesConfigured_DoesNothing()
    {
        _mockPrizeRepo.Setup(r => r.GetByChallengeIdAsync(_challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prize>());

        await _handler.Handle(MakeEvent(), CancellationToken.None);

        _mockEngine.Verify(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPublisher.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- Scenario 2: Prize selected and awarded ---
    [Fact]
    public async Task Handle_WhenPrizeSelected_CallsAwarderAndPublishesPrizeAwardedEvent()
    {
        var prize = MakePrize();
        var userPrize = MakeUserPrize(prize);

        _mockPrizeRepo.Setup(r => r.GetByChallengeIdAsync(_challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prize> { prize });
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prize);
        _mockAwarder.Setup(a => a.AwardAsync(prize, It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPrize);

        await _handler.Handle(MakeEvent(), CancellationToken.None);

        _mockAwarder.Verify(a => a.AwardAsync(prize, It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPublisher.Verify(p => p.Publish(It.Is<PrizeAwardedEvent>(e => e.UserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Scenario 3: Engine returns null (AllowNoWin) ---
    [Fact]
    public async Task Handle_WhenEngineReturnsNull_DoesNotAwardAndPublishesNoPrizeEvent()
    {
        var prize = MakePrize();
        prize.AllowNoWin = true;

        _mockPrizeRepo.Setup(r => r.GetByChallengeIdAsync(_challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prize> { prize });
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prize?)null);

        await _handler.Handle(MakeEvent(), CancellationToken.None);

        _mockAwarder.Verify(a => a.AwardAsync(It.IsAny<Prize>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPublisher.Verify(p => p.Publish(It.IsAny<NoPrizeEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Scenario 4: PrizeAwardedEvent published with correct data ---
    [Fact]
    public async Task Handle_WhenPrizeAwarded_PublishesCorrectPrizeAwardedEventData()
    {
        var prize = MakePrize(type: PrizeType.Points);
        var userPrize = new UserPrize
        {
            Id = Guid.NewGuid(), UserId = _userId, PrizeId = prize.Id,
            PrizeType = PrizeType.Points, PointsAwarded = 200
        };

        _mockPrizeRepo.Setup(r => r.GetByChallengeIdAsync(_challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prize> { prize });
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prize);
        _mockAwarder.Setup(a => a.AwardAsync(prize, It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPrize);

        PrizeAwardedEvent? capturedEvent = null;
        _mockPublisher.Setup(p => p.Publish(It.IsAny<PrizeAwardedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => capturedEvent = e as PrizeAwardedEvent)
            .Returns(Task.CompletedTask);

        await _handler.Handle(MakeEvent(), CancellationToken.None);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.UserId.Should().Be(_userId);
        capturedEvent.PrizeId.Should().Be(prize.Id);
        capturedEvent.PrizeType.Should().Be(PrizeType.Points);
        capturedEvent.PointsAwarded.Should().Be(200);
    }

    // --- Scenario 5: SubmissionId passed to context ---
    [Fact]
    public async Task Handle_WhenSubmissionIdSet_PassesItToContext()
    {
        var submissionId = Guid.NewGuid();
        var prize = MakePrize();
        var userPrize = MakeUserPrize(prize);

        _mockPrizeRepo.Setup(r => r.GetByChallengeIdAsync(_challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prize> { prize });
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prize);
        _mockAwarder.Setup(a => a.AwardAsync(prize, It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPrize);

        PrizeSelectionContext? capturedCtx = null;
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Prize>, PrizeSelectionContext, CancellationToken>((_, ctx, _) => capturedCtx = ctx)
            .ReturnsAsync(prize);

        await _handler.Handle(MakeEvent(submissionId), CancellationToken.None);

        capturedCtx!.SubmissionId.Should().Be(submissionId);
    }

    // --- Scenario 6: NoPrizeEvent has correct reason ---
    [Fact]
    public async Task Handle_WhenNoPrize_NoPrizeEventContainsChallengeId()
    {
        _mockPrizeRepo.Setup(r => r.GetByChallengeIdAsync(_challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prize> { MakePrize() });
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prize?)null);

        NoPrizeEvent? capturedEvent = null;
        _mockPublisher.Setup(p => p.Publish(It.IsAny<NoPrizeEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((e, _) => capturedEvent = e as NoPrizeEvent)
            .Returns(Task.CompletedTask);

        await _handler.Handle(MakeEvent(), CancellationToken.None);

        capturedEvent!.ChallengeId.Should().Be(_challengeId);
        capturedEvent.UserId.Should().Be(_userId);
    }

    // --- Scenario 7: Engine receives correct challenge id ---
    [Fact]
    public async Task Handle_PassesCorrectChallengeIdToEngine()
    {
        var prize = MakePrize();
        _mockPrizeRepo.Setup(r => r.GetByChallengeIdAsync(_challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prize> { prize });
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Prize?)null);

        PrizeSelectionContext? capturedCtx = null;
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Prize>, PrizeSelectionContext, CancellationToken>((_, ctx, _) => capturedCtx = ctx)
            .ReturnsAsync((Prize?)null);

        await _handler.Handle(MakeEvent(), CancellationToken.None);

        capturedCtx!.ChallengeId.Should().Be(_challengeId);
        capturedCtx.UserId.Should().Be(_userId);
    }

    // --- Scenario 8: Product prize selected and awarded ---
    [Fact]
    public async Task Handle_WhenProductPrizeSelected_AwardsAndPublishesEvent()
    {
        var prize = MakePrize(remaining: 1, type: PrizeType.Product);
        var userPrize = new UserPrize
        {
            Id = Guid.NewGuid(), UserId = _userId, PrizeId = prize.Id,
            PrizeType = PrizeType.Product, Code = "VXP-PROD-ABCDEFGH"
        };

        _mockPrizeRepo.Setup(r => r.GetByChallengeIdAsync(_challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Prize> { prize });
        _mockEngine.Setup(e => e.TrySelectPrizeAsync(It.IsAny<IReadOnlyList<Prize>>(), It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(prize);
        _mockAwarder.Setup(a => a.AwardAsync(prize, It.IsAny<PrizeSelectionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPrize);

        await _handler.Handle(MakeEvent(), CancellationToken.None);

        _mockPublisher.Verify(p => p.Publish(It.Is<PrizeAwardedEvent>(e => e.PrizeType == PrizeType.Product), It.IsAny<CancellationToken>()), Times.Once);
    }
}
