using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Dynamics;

/// <summary>
/// Strategy for the Survey dynamic.
/// Surveys are always marked as successfully completed upon submission.
/// Answer storage and prize assignment are handled downstream via ChallengeCompletedEvent.
/// </summary>
public class SurveyStrategy : IDynamicStrategy
{
    public string DynamicType => Domain.Enums.DynamicType.Survey.ToString();

    public Task<DynamicResult> ExecuteAsync(DynamicContext context, CancellationToken cancellationToken)
    {
        // Surveys are complete upon submission — success is always true
        var payload = new Dictionary<string, object>
        {
            { "Score", 100 }
        };

        return Task.FromResult(new DynamicResult
        {
            Success = true,
            Message = "Survey submitted successfully.",
            Payload = payload
        });
    }
}
