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

public class TriviaStrategyTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDynamicChallengeRepository> _mockChallengeRepo;
    private readonly TriviaStrategy _strategy;

    public TriviaStrategyTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockChallengeRepo = new Mock<IDynamicChallengeRepository>();
        _mockUnitOfWork.Setup(u => u.DynamicChallenges).Returns(_mockChallengeRepo.Object);
        _strategy = new TriviaStrategy(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithCorrectAnswers_ShouldCalculateCorrectScoreAndReturnSuccess()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 3 questions, each 10 points (total 30 points). Threshold = 70.
        // User answers 3/3 correct. Score = 100%. PassingScore = 70.
        var configJson = @"
        {
            ""passingScore"": 70,
            ""questions"": [
                { ""id"": ""q1"", ""correctAnswer"": ""A"", ""points"": 10 },
                { ""id"": ""q2"", ""correctAnswer"": ""B"", ""points"": 10 },
                { ""id"": ""q3"", ""correctAnswer"": ""C"", ""points"": 10 }
            ]
        }";

        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            ConfigurationJson = configJson,
            Type = Domain.Enums.DynamicType.Trivia
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        var inputs = new Dictionary<string, string>
        {
            { "q1", "A" },
            { "q2", "B" },
            { "q3", "C" }
        };

        var context = new DynamicContext
        {
            DynamicId = challengeId,
            UserId = userId,
            Inputs = inputs
        };

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Trivia passed!");
        result.Payload.Should().BeOfType<Dictionary<string, object>>();

        var payload = result.Payload as Dictionary<string, object>;
        payload.Should().NotBeNull();
        payload!["Score"].Should().Be(100);
        payload["Total"].Should().Be(30);
        payload["CorrectCount"].Should().Be(3);
        payload["PassingScore"].Should().Be(70);
    }

    [Fact]
    public async Task ExecuteAsync_WithSomeIncorrectAnswers_ShouldCalculateCorrectScoreAndReturnFailureIfBelowThreshold()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 3 questions, each 10 points (total 30 points). Threshold = 70.
        // User answers 1/3 correct. Score = 33%. PassingScore = 70.
        var configJson = @"
        {
            ""passingScore"": 70,
            ""questions"": [
                { ""id"": ""q1"", ""correctAnswer"": ""A"", ""points"": 10 },
                { ""id"": ""q2"", ""correctAnswer"": ""B"", ""points"": 10 },
                { ""id"": ""q3"", ""correctAnswer"": ""C"", ""points"": 10 }
            ]
        }";

        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            ConfigurationJson = configJson,
            Type = Domain.Enums.DynamicType.Trivia
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        var inputs = new Dictionary<string, string>
        {
            { "q1", "A" }, // Correct
            { "q2", "Wrong" }, // Incorrect
            { "q3", "Wrong" } // Incorrect
        };

        var context = new DynamicContext
        {
            DynamicId = challengeId,
            UserId = userId,
            Inputs = inputs
        };

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Trivia failed");

        var payload = result.Payload as Dictionary<string, object>;
        payload.Should().NotBeNull();
        payload!["Score"].Should().Be(33); // 10/30 = 33.33% -> Round to 33
        payload["Total"].Should().Be(30);
        payload["CorrectCount"].Should().Be(1);
        payload["PassingScore"].Should().Be(70);
    }

    [Fact]
    public async Task ExecuteAsync_WithScoreExactlyAtThreshold_ShouldReturnSuccess()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 4 questions, each 10 points (total 40 points). Threshold = 75.
        // User answers 3/4 correct. Score = 75%. PassingScore = 75.
        var configJson = @"
        {
            ""passingScore"": 75,
            ""questions"": [
                { ""id"": ""q1"", ""correctAnswer"": ""A"", ""points"": 10 },
                { ""id"": ""q2"", ""correctAnswer"": ""B"", ""points"": 10 },
                { ""id"": ""q3"", ""correctAnswer"": ""C"", ""points"": 10 },
                { ""id"": ""q4"", ""correctAnswer"": ""D"", ""points"": 10 }
            ]
        }";

        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            ConfigurationJson = configJson,
            Type = Domain.Enums.DynamicType.Trivia
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        var inputs = new Dictionary<string, string>
        {
            { "q1", "A" },
            { "q2", "B" },
            { "q3", "C" },
            { "q4", "Wrong" }
        };

        var context = new DynamicContext
        {
            DynamicId = challengeId,
            UserId = userId,
            Inputs = inputs
        };

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Trivia passed!");

        var payload = result.Payload as Dictionary<string, object>;
        payload.Should().NotBeNull();
        payload!["Score"].Should().Be(75);
        payload["CorrectCount"].Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_WhenChallengeNotFound_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DynamicChallenge?)null);

        var context = new DynamicContext
        {
            DynamicId = challengeId,
            UserId = Guid.NewGuid(),
            Inputs = new Dictionary<string, string>()
        };

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Trivia challenge not found.");
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigJsonIsEmpty_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            ConfigurationJson = "",
            Type = Domain.Enums.DynamicType.Trivia
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        var context = new DynamicContext
        {
            DynamicId = challengeId,
            UserId = Guid.NewGuid(),
            Inputs = new Dictionary<string, string>()
        };

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Trivia configuration is empty.");
    }
}
