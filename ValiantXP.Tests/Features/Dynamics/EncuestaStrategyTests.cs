using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using ValiantXP.Domain.Entities;
using ValiantXP.Infrastructure.Dynamics;
using Xunit;

namespace ValiantXP.Tests.Features.Dynamics;

public class EncuestaStrategyTests
{
    private readonly EncuestaStrategy _strategy;

    public EncuestaStrategyTests()
    {
        _strategy = new EncuestaStrategy();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteImmediatelyAndSuccessfully()
    {
        // Arrange
        var context = new DynamicContext
        {
            DynamicId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Inputs = new Dictionary<string, string>
            {
                { "opinion", "Excellent" }
            }
        };

        // Act
        var result = await _strategy.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Survey submitted successfully.");
        result.Payload.Should().BeOfType<Dictionary<string, object>>();

        var payload = result.Payload as Dictionary<string, object>;
        payload.Should().NotBeNull();
        payload!["Score"].Should().Be(100);
    }
}
