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

public class ChainingIntegrationTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDynamicChallengeRepository> _mockChallengeRepo;
    private readonly Mock<ICampaignRepository> _mockCampaignRepo;
    private readonly Mock<IUserChallengeProgressRepository> _mockProgressRepo;
    private readonly Mock<IUserPrizeRepository> _mockUserPrizeRepo;
    private readonly Mock<IDynamicService> _mockDynamicService;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly SubmitChallengeCommandHandler _handler;

    public ChainingIntegrationTests()
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
    public async Task Handle_WhenChallengeHasNextChallengeId_AndSucceeds_ShouldReturnNextChallengeId()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var nextChallengeId = Guid.NewGuid(); // the next challenge in the chain
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
            Type = DynamicType.Code,
            NextChallengeId = nextChallengeId // chain set
        };

        var inputs = new Dictionary<string, string> { { "code", "CHAIN-CODE-001" } };
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
            Message = "Code 'CHAIN-CODE-001' successfully redeemed!",
            Payload = new Dictionary<string, object>
            {
                { "CodeNumber", "CHAIN-CODE-001" },
                { "CampaignId", campaignId }
            }
        };

        _mockDynamicService
            .Setup(s => s.ProcessDynamicAsync(challengeId, userId, inputs, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dynamicResult);

        _mockUserPrizeRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserPrize>());

        _mockProgressRepo
            .Setup(r => r.AddAsync(It.IsAny<UserChallengeProgress>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeTrue();

        // KEY ASSERTION: NextChallengeId must be populated for chaining
        result.Value.NextChallengeId.Should().Be(nextChallengeId);
    }

    [Fact]
    public async Task Handle_WhenChallengeHasNoNextChallengeId_AndSucceeds_ShouldReturnNullNextChallengeId()
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
            Type = DynamicType.Trivia,
            NextChallengeId = null // no chain
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
            Payload = new Dictionary<string, object> { { "Score", 100 } }
        };

        _mockDynamicService
            .Setup(s => s.ProcessDynamicAsync(challengeId, userId, inputs, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dynamicResult);

        _mockUserPrizeRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserPrize>());

        _mockProgressRepo
            .Setup(r => r.AddAsync(It.IsAny<UserChallengeProgress>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeTrue();
        result.Value.NextChallengeId.Should().BeNull(); // no chain
    }

    [Fact]
    public async Task Handle_WhenChallengeHasNextChallengeId_ButFails_ShouldReturnNullNextChallengeId()
    {
        // Arrange - even if there is a NextChallengeId, if the challenge failed, chaining should NOT happen
        var challengeId = Guid.NewGuid();
        var nextChallengeId = Guid.NewGuid();
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
            Type = DynamicType.Code,
            NextChallengeId = nextChallengeId
        };

        var inputs = new Dictionary<string, string> { { "code", "WRONG-CODE" } };
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
            Success = false,
            Message = "The provided code does not exist or is not valid for this campaign.",
            Payload = null
        };

        _mockDynamicService
            .Setup(s => s.ProcessDynamicAsync(challengeId, userId, inputs, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dynamicResult);

        _mockProgressRepo
            .Setup(r => r.AddAsync(It.IsAny<UserChallengeProgress>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();        // operation itself succeeded (no exception)
        result.Value!.Success.Should().BeFalse();  // but the challenge attempt failed
        result.Value.NextChallengeId.Should().BeNull(); // no chaining on failure
    }
}
