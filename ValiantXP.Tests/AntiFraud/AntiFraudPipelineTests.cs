using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using ValiantXP.Application.AntiFraud;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using Xunit;

namespace ValiantXP.Tests.AntiFraud;

public class AntiFraudPipelineTests
{
    private static AntiFraudContext BuildContext(DynamicType type = DynamicType.Codigo) => new()
    {
        UserId = Guid.NewGuid(),
        ChallengeId = Guid.NewGuid(),
        CampaignId = Guid.NewGuid(),
        ChallengeType = type,
        RemoteIp = "127.0.0.1",
        Inputs = new Dictionary<string, string> { { "code", "TEST123" } },
        Config = new AntiFraudCampaignConfig(),
        CampaignStartDate = DateTime.UtcNow.AddDays(-1),
        CampaignEndDate = DateTime.UtcNow.AddDays(30)
    };

    [Fact]
    public async Task RunAsync_NoRules_CompletesWithoutException()
    {
        var pipeline = new AntiFraudPipeline(Array.Empty<IAntiFraudRule>());
        var act = async () => await pipeline.RunAsync(BuildContext(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunAsync_AllRulesPass_CompletesWithoutException()
    {
        var rule1 = new Mock<IAntiFraudRule>();
        rule1.Setup(r => r.ApplicableType).Returns((DynamicType?)null);
        rule1.Setup(r => r.Order).Returns(10);
        rule1.Setup(r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var rule2 = new Mock<IAntiFraudRule>();
        rule2.Setup(r => r.ApplicableType).Returns((DynamicType?)null);
        rule2.Setup(r => r.Order).Returns(20);
        rule2.Setup(r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var pipeline = new AntiFraudPipeline(new[] { rule2.Object, rule1.Object }); // intentionally unordered

        var act = async () => await pipeline.RunAsync(BuildContext(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunAsync_RuleThrowsAntiFraud_PropagatesException()
    {
        var rule = new Mock<IAntiFraudRule>();
        rule.Setup(r => r.ApplicableType).Returns((DynamicType?)null);
        rule.Setup(r => r.Order).Returns(10);
        rule.Setup(r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(AntiFraudException.CodeAlreadyUsed("TEST123"));

        var pipeline = new AntiFraudPipeline(new[] { rule.Object });

        var act = async () => await pipeline.RunAsync(BuildContext(), CancellationToken.None);
        await act.Should().ThrowAsync<AntiFraudException>()
            .WithMessage("*already been redeemed*");
    }

    [Fact]
    public async Task RunAsync_RuleForDifferentType_IsSkipped()
    {
        var triviaRule = new Mock<IAntiFraudRule>();
        triviaRule.Setup(r => r.ApplicableType).Returns(DynamicType.Trivia);
        triviaRule.Setup(r => r.Order).Returns(10);

        var pipeline = new AntiFraudPipeline(new[] { triviaRule.Object });

        // Context is Codigo — Trivia rule should be skipped, no exception
        var act = async () => await pipeline.RunAsync(BuildContext(DynamicType.Codigo), CancellationToken.None);
        await act.Should().NotThrowAsync();

        triviaRule.Verify(
            r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_RulesExecutedInAscendingOrder()
    {
        var executionOrder = new List<int>();

        var ruleA = new Mock<IAntiFraudRule>();
        ruleA.Setup(r => r.ApplicableType).Returns((DynamicType?)null);
        ruleA.Setup(r => r.Order).Returns(30);
        ruleA.Setup(r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()))
             .Returns(() => { executionOrder.Add(30); return Task.CompletedTask; });

        var ruleB = new Mock<IAntiFraudRule>();
        ruleB.Setup(r => r.ApplicableType).Returns((DynamicType?)null);
        ruleB.Setup(r => r.Order).Returns(5);
        ruleB.Setup(r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()))
             .Returns(() => { executionOrder.Add(5); return Task.CompletedTask; });

        var ruleC = new Mock<IAntiFraudRule>();
        ruleC.Setup(r => r.ApplicableType).Returns((DynamicType?)null);
        ruleC.Setup(r => r.Order).Returns(20);
        ruleC.Setup(r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()))
             .Returns(() => { executionOrder.Add(20); return Task.CompletedTask; });

        var pipeline = new AntiFraudPipeline(new[] { ruleA.Object, ruleB.Object, ruleC.Object });
        await pipeline.RunAsync(BuildContext(), CancellationToken.None);

        executionOrder.Should().Equal(5, 20, 30);
    }

    [Fact]
    public async Task RunAsync_FirstRuleFails_SubsequentRulesNotCalled()
    {
        var failingRule = new Mock<IAntiFraudRule>();
        failingRule.Setup(r => r.ApplicableType).Returns((DynamicType?)null);
        failingRule.Setup(r => r.Order).Returns(10);
        failingRule.Setup(r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(AntiFraudException.CampaignNotActive());

        var secondRule = new Mock<IAntiFraudRule>();
        secondRule.Setup(r => r.ApplicableType).Returns((DynamicType?)null);
        secondRule.Setup(r => r.Order).Returns(20);

        var pipeline = new AntiFraudPipeline(new[] { failingRule.Object, secondRule.Object });

        await Assert.ThrowsAsync<AntiFraudException>(
            () => pipeline.RunAsync(BuildContext(), CancellationToken.None));

        secondRule.Verify(
            r => r.ValidateAsync(It.IsAny<AntiFraudContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
