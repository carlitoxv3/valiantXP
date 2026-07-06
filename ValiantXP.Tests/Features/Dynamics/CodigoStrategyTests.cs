using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Dynamics;
using Xunit;

namespace ValiantXP.Tests.Features.Dynamics;

public class CodeStrategyTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICodeRepository> _mockCodeRepo;
    private readonly Mock<IDynamicChallengeRepository> _mockChallengeRepo;
    private readonly Mock<IUserChallengeProgressRepository> _mockProgressRepo;
    private readonly CodeStrategy _strategy;

    public CodeStrategyTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCodeRepo = new Mock<ICodeRepository>();
        _mockChallengeRepo = new Mock<IDynamicChallengeRepository>();
        _mockProgressRepo = new Mock<IUserChallengeProgressRepository>();
        _mockUnitOfWork.Setup(u => u.Codes).Returns(_mockCodeRepo.Object);
        _mockUnitOfWork.Setup(u => u.DynamicChallenges).Returns(_mockChallengeRepo.Object);
        // Default: challenge has no position_based config (standard mode)
        _mockChallengeRepo
            .Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DynamicChallenge?)null);
        _strategy = new CodeStrategy(_mockUnitOfWork.Object, _mockProgressRepo.Object);
    }

    [Fact]
    public async Task ExecuteAsync_NullCode_ReturnsFailure()
    {
        // Arrange
        var context = new DynamicContext
        {
            DynamicId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Inputs = new Dictionary<string, string>() // no 'code' key
        };

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No code was submitted");
        _mockCodeRepo.Verify(r => r.GetByCodeNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCode_ReturnsFailure()
    {
        // Arrange
        var context = new DynamicContext
        {
            DynamicId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Inputs = new Dictionary<string, string> { { "code", "   " } } // whitespace only
        };

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No code was submitted");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCode_ReturnsFailure()
    {
        // Arrange
        var context = new DynamicContext
        {
            DynamicId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Inputs = new Dictionary<string, string> { { "code", "NONEXISTENT123" } }
        };

        _mockCodeRepo
            .Setup(r => r.GetByCodeNumberAsync("NONEXISTENT123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Code?)null);

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("does not exist");
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyUsedCode_ReturnsFailure()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var usedCode = new Code
        {
            Id = Guid.NewGuid(),
            CodeNumber = "USED-CODE-001",
            CampaignId = campaignId,
            UsedAt = DateTime.UtcNow.AddHours(-1), // already redeemed
            UserId = Guid.NewGuid()
        };

        var context = new DynamicContext
        {
            DynamicId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Inputs = new Dictionary<string, string> { { "code", "USED-CODE-001" } }
        };

        _mockCodeRepo
            .Setup(r => r.GetByCodeNumberAsync("USED-CODE-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usedCode);

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already been redeemed");
        _mockCodeRepo.Verify(r => r.UpdateAsync(It.IsAny<Code>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCode_MarksUsedAndReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var validCode = new Code
        {
            Id = Guid.NewGuid(),
            CodeNumber = "VALID-CODE-001",
            CampaignId = campaignId,
            UsedAt = null, // not yet redeemed
            UserId = null
        };

        var context = new DynamicContext
        {
            DynamicId = Guid.NewGuid(),
            UserId = userId,
            Inputs = new Dictionary<string, string> { { "code", "VALID-CODE-001" } }
        };

        _mockCodeRepo
            .Setup(r => r.GetByCodeNumberAsync("VALID-CODE-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(validCode);

        _mockCodeRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Code>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var beforeCall = DateTime.UtcNow;

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("VALID-CODE-001");
        result.Message.Should().Contain("successfully redeemed");

        validCode.UsedAt.Should().NotBeNull();
        validCode.UsedAt.Should().BeOnOrAfter(beforeCall);
        validCode.UserId.Should().Be(userId);

        var payload = result.Payload as Dictionary<string, object>;
        payload.Should().NotBeNull();
        payload!["CodeNumber"].Should().Be("VALID-CODE-001");
        payload["CampaignId"].Should().Be(campaignId);

        _mockCodeRepo.Verify(r => r.UpdateAsync(validCode, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCodeWithLeadingTrailingSpaces_TrimsAndSucceeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var validCode = new Code
        {
            Id = Guid.NewGuid(),
            CodeNumber = "TRIM-CODE-001",
            CampaignId = campaignId,
            UsedAt = null,
            UserId = null
        };

        var context = new DynamicContext
        {
            DynamicId = Guid.NewGuid(),
            UserId = userId,
            Inputs = new Dictionary<string, string> { { "code", "  TRIM-CODE-001  " } }
        };

        _mockCodeRepo
            .Setup(r => r.GetByCodeNumberAsync("TRIM-CODE-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(validCode);

        _mockCodeRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Code>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockCodeRepo.Verify(r => r.GetByCodeNumberAsync("TRIM-CODE-001", It.IsAny<CancellationToken>()), Times.Once);
    }
}
