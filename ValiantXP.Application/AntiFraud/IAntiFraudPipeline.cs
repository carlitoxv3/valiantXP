using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.AntiFraud;

namespace ValiantXP.Application.AntiFraud;

/// <summary>
/// Runs all registered IAntiFraudRule instances applicable to the given context
/// in ascending Order. Throws AntiFraudException on the first rule violation.
/// </summary>
public interface IAntiFraudPipeline
{
    /// <summary>
    /// Executes the anti-fraud pipeline for the given context.
    /// Throws <see cref="Domain.Exceptions.AntiFraudException"/> if any rule rejects the request.
    /// </summary>
    Task RunAsync(AntiFraudContext context, CancellationToken cancellationToken);
}
