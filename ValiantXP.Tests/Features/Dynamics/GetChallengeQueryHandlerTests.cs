using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.Features.Dynamics.Queries;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using Xunit;

namespace ValiantXP.Tests.Features.Dynamics;

public class GetChallengeQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDynamicChallengeRepository> _mockChallengeRepo;
    private readonly GetChallengeQueryHandler _handler;

    public GetChallengeQueryHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockChallengeRepo = new Mock<IDynamicChallengeRepository>();
        _mockUnitOfWork.Setup(u => u.DynamicChallenges).Returns(_mockChallengeRepo.Object);
        _handler = new GetChallengeQueryHandler(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithValidActiveChallenge_ShouldSanitizeCorrectAnswersFromConfigurationJson()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var rawJson = @"
        {
            ""passingScore"": 70,
            ""questions"": [
                {
                    ""id"": ""q1"",
                    ""text"": ""What is 2+2?"",
                    ""options"": [""3"", ""4"", ""5""],
                    ""correctAnswer"": ""4"",
                    ""answerKey"": ""B"",
                    ""points"": 10
                },
                {
                    ""id"": ""q2"",
                    ""text"": ""What is capital of France?"",
                    ""options"": [""Paris"", ""London""],
                    ""correct_answer"": ""Paris"",
                    ""userAnswerField"": ""some_input"",
                    ""points"": 20
                }
            ]
        }";

        var challenge = new DynamicChallenge
        {
            Id = challengeId,
            CampaignId = Guid.NewGuid(),
            Type = Domain.Enums.DynamicType.Trivia,
            Name = "Math & Geography Trivia",
            ConfigurationJson = rawJson,
            IsActive = true
        };

        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        var query = new GetChallengeQuery(challengeId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(challengeId);
        result.Value.ConfigurationJson.Should().NotBeNullOrWhiteSpace();

        // The sanitized JSON should NOT contain words like "correctAnswer", "answerKey", "correct_answer"
        result.Value.ConfigurationJson.Should().NotContain("correctAnswer");
        result.Value.ConfigurationJson.Should().NotContain("correct_answer");
        result.Value.ConfigurationJson.Should().NotContain("answerKey");

        // But it SHOULD keep safe properties like "id", "text", "options", "points", "passingScore"
        result.Value.ConfigurationJson.Should().Contain("q1");
        result.Value.ConfigurationJson.Should().Contain("What is 2");
        result.Value.ConfigurationJson.Should().Contain("options");
        result.Value.ConfigurationJson.Should().Contain("points");
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

        var query = new GetChallengeQuery(challengeId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Challenge is inactive.");
    }

    [Fact]
    public async Task Handle_WhenChallengeDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        _mockChallengeRepo
            .Setup(r => r.GetAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DynamicChallenge?)null);

        var query = new GetChallengeQuery(challengeId);

        // Act
        var resultDto = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultDto.IsSuccess.Should().BeFalse();
        resultDto.Error.Should().Be("Challenge not found.");
    }
}
