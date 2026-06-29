using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Moq;
using ValiantXP.Application.AntiFraud;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.Features.Dynamics.Commands;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;
using Xunit;

namespace ValiantXP.Tests.Features.Dynamics;

public class SubmitChallengeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDynamicChallengeRepository> _mockChallengeRepo;
    private readonly Mock<ICampaignRepository> _mockCampaignRepo;
    private readonly Mock<IUserChallengeProgressRepository> _mockProgressRepo;
    private readonly Mock<IUserPrizeRepository> _mockUserPrizeRepo;
    private readonly Mock<IDynamicService> _mockDynamicService;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly SubmitChallengeCommandHandler _handler;

    public SubmitChallengeCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockChallengeRepo = new Mock<IDynamicChallengeRepository>();
        _mockCampaignRepo = new Mock<ICampaignRepository>();
        _mockProgressRepo = new Mock<IUserChallengeProgressRepository>();
        _mockUserPrizeRepo = new Mock<IUserPrizeRepository>();

        _mockUnitOfWork.Setup(u => u.DynamicChallenges).Returns(_mockChallengeRepo.Object);
        _mockUnitOfWork.Setup(u => u.Campaigns).Returns(_mockCampaignRepo.Object);
        _mockUnitOfWork.Setup(u => u.UserChallengeProgresses).Returns(_mockProgressRepo.Object);
        _mockUnitOfWork.Setup(u => u.UserPrizes).Returns(_mockUserPrizeRepo.Object);

        _mockDynamicService = new Mock<IDynamicService>();
        _mockPublisher = new Mock<IPublisher>();

        // No-op anti-fraud pipeline for tests that don't need fraud checks
        var mockAntiFraud = new Mock<IAntiFraudPipeline>();
        mockAntiFraud
            .Setup(p => p.RunAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SubmitChallengeCommandHandler(
            _mockDynamicService.Object,
            _mockUnitOfWork.Object,
            _mockPublisher.Object,
            mockAntiFraud.Object
        );
    }

    [Fact]
    public async Task Handle_FirstAttempt_Success_ShouldCreateProgressWithAttempts1_SetStatusToCompleted_PublishCompletedEvent()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var campaign = new Campaign
        {
            Id = campaignId,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            CampaignId = campaignId,
            IsActive = true,
            Type = DynamicType.Trivia
        };

        var inputs = new Dictionary<string, string> { { "q1", "A" } };
        var command = new SubmitChallengeCommand(challengeId, userId, inputs);

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockCampaignRepo
            .Setup(r => r.GetAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        _mockProgressRepo
            .Setup(r => r.GetByUserAndChallengeAsync(userId, challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserChallengeProgress?)null);

        var dynamicResult = new DynamicResult
        {
            Success = true,
            Message = "Trivia passed!",
            Payload = new Dictionary<string, object> { { "Score", 90 } }
        };

        _mockDynamicService
            .Setup(s => s.ProcessDynamicAsync(challengeId, userId, inputs, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dynamicResult);

        _mockUserPrizeRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserPrize>());

        UserChallengeProgress? savedProgress = null;
        _mockProgressRepo
            .Setup(r => r.AddAsync(It.IsAny<UserChallengeProgress>(), It.IsAny<CancellationToken>()))
            .Callback<UserChallengeProgress, CancellationToken>((p, _) => savedProgress = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeTrue();
        result.Value.Message.Should().Be("Trivia passed!");

        savedProgress.Should().NotBeNull();
        savedProgress!.UserId.Should().Be(userId);
        savedProgress.DynamicChallengeId.Should().Be(challengeId);
        savedProgress.Attempts.Should().Be(1);
        savedProgress.Score.Should().Be(90);
        savedProgress.Status.Should().Be(ChallengeStatus.Completed);
        savedProgress.CompletedAt.Should().NotBeNull();

        _mockProgressRepo.Verify(r => r.AddAsync(It.IsAny<UserChallengeProgress>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockPublisher.Verify(p => p.Publish(It.IsAny<ChallengeCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SubsequentAttempt_Failure_ShouldIncrementAttempts_SetStatusToFailed_NotPublishCompletedEvent()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var campaign = new Campaign
        {
            Id = campaignId,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            CampaignId = campaignId,
            IsActive = true,
            Type = DynamicType.Trivia
        };

        var inputs = new Dictionary<string, string> { { "q1", "Wrong" } };
        var command = new SubmitChallengeCommand(challengeId, userId, inputs);

        var existingProgress = new UserChallengeProgress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DynamicChallengeId = challengeId,
            Attempts = 1,
            Score = 30,
            Status = ChallengeStatus.Failed
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockCampaignRepo
            .Setup(r => r.GetAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        _mockProgressRepo
            .Setup(r => r.GetByUserAndChallengeAsync(userId, challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProgress);

        var dynamicResult = new DynamicResult
        {
            Success = false,
            Message = "Trivia failed.",
            Payload = new Dictionary<string, object> { { "Score", 50 } }
        };

        _mockDynamicService
            .Setup(s => s.ProcessDynamicAsync(challengeId, userId, inputs, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dynamicResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeFalse();
        result.Value.Message.Should().Be("Trivia failed.");

        existingProgress.Attempts.Should().Be(2);
        existingProgress.Score.Should().Be(50);
        existingProgress.Status.Should().Be(ChallengeStatus.Failed);
        existingProgress.CompletedAt.Should().BeNull();

        _mockProgressRepo.Verify(r => r.UpdateAsync(existingProgress, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockPublisher.Verify(p => p.Publish(It.IsAny<ChallengeCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyCompleted_ShouldReturnFailureResultWithoutRunningDynamicExecution()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var campaign = new Campaign
        {
            Id = campaignId,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            CampaignId = campaignId,
            IsActive = true
        };

        var command = new SubmitChallengeCommand(challengeId, userId, new Dictionary<string, string>());

        var existingProgress = new UserChallengeProgress
        {
            UserId = userId,
            DynamicChallengeId = challengeId,
            Attempts = 1,
            Status = ChallengeStatus.Completed
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockCampaignRepo
            .Setup(r => r.GetAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        _mockProgressRepo
            .Setup(r => r.GetByUserAndChallengeAsync(userId, challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProgress);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("You have already completed this challenge.");

        _mockDynamicService.Verify(s => s.ProcessDynamicAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenChallengeIsInactive_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            IsActive = false
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        var command = new SubmitChallengeCommand(challengeId, Guid.NewGuid(), new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Challenge is inactive.");
    }

    [Fact]
    public async Task Handle_WhenCampaignIsInactive_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();

        var campaign = new Campaign
        {
            Id = campaignId,
            IsActive = false
        };

        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            CampaignId = campaignId,
            IsActive = true
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockCampaignRepo
            .Setup(r => r.GetAsync(campaignId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);

        var command = new SubmitChallengeCommand(challengeId, Guid.NewGuid(), new Dictionary<string, string>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Campaign is inactive.");
    }
}
