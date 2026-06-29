using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Order 10 — Codigo only.
/// Verifies the submitted code exists in the database.
/// Equivalent to PromoHub ExchangeCode SP: first check before any other action.
/// </summary>
public sealed class CodeExistsRule : IAntiFraudRule
{
    private readonly IUnitOfWork _unitOfWork;
    public DynamicType? ApplicableType => DynamicType.Code;
    public int Order => 10;

    public CodeExistsRule(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        var code = context.SubmittedCode;
        if (string.IsNullOrWhiteSpace(code)) return; // CodeNotFound handled upstream

        var entity = await _unitOfWork.Codes.GetByCodeNumberAsync(code, cancellationToken);
        if (entity is null)
            throw AntiFraudException.CodeNotFound(code);
    }
}
