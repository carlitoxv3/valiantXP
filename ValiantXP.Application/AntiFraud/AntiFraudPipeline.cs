using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.AntiFraud;

namespace ValiantXP.Application.AntiFraud;

/// <summary>
/// Default implementation of IAntiFraudPipeline.
/// Iterates all registered IAntiFraudRule instances ordered by Rule.Order,
/// skipping rules whose ApplicableType doesn't match the current dynamic type.
/// Stops and rethrows on the first AntiFraudException.
/// </summary>
public sealed class AntiFraudPipeline : IAntiFraudPipeline
{
    private readonly IReadOnlyList<IAntiFraudRule> _rules;

    public AntiFraudPipeline(IEnumerable<IAntiFraudRule> rules)
    {
        // Sort once at construction time; rules are typically singletons/scoped.
        _rules = rules.OrderBy(r => r.Order).ToList();
    }

    public async Task RunAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        foreach (var rule in _rules)
        {
            // Skip rules that are scoped to a different dynamic type
            if (rule.ApplicableType.HasValue && rule.ApplicableType.Value != context.ChallengeType)
                continue;

            // AntiFraudException propagates up — handler decides whether to record the attempt
            await rule.ValidateAsync(context, cancellationToken);
        }
    }
}
