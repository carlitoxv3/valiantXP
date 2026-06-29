using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.AntiFraud;

/// <summary>
/// Contract for a single anti-fraud validation rule.
///
/// Rules are executed in ascending Order by AntiFraudPipeline.
/// A rule throws AntiFraudException to halt the pipeline.
/// Rules that only apply to one DynamicType set ApplicableType accordingly;
/// rules with null ApplicableType run for every dynamic type.
/// </summary>
public interface IAntiFraudRule
{
    /// <summary>
    /// The dynamic type this rule applies to.
    /// Null means the rule applies to ALL dynamic types.
    /// </summary>
    DynamicType? ApplicableType { get; }

    /// <summary>
    /// Execution order (lower runs first). Use multiples of 10 to allow injection of
    /// campaign-specific rules between base rules without renumbering.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Validates the context. Throws <see cref="Exceptions.AntiFraudException"/> if fraud is detected.
    /// Should not throw any other exception type for fraud conditions.
    /// </summary>
    Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken);
}
