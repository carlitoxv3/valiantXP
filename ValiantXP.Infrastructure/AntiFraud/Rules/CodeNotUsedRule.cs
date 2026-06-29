using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Order 20 — Codigo only.
/// Verifies the code has not already been redeemed (UsedAt == null).
/// Equivalent to PromoHub ExchangeCode SP check #2.
/// </summary>
public sealed class CodeNotUsedRule : IAntiFraudRule
{
    private readonly IUnitOfWork _unitOfWork;
    public DynamicType? ApplicableType => DynamicType.Codigo;
    public int Order => 20;

    public CodeNotUsedRule(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        var code = context.SubmittedCode;
        if (string.IsNullOrWhiteSpace(code)) return;

        var entity = await _unitOfWork.Codes.GetByCodeNumberAsync(code, cancellationToken);
        if (entity is not null && entity.UsedAt.HasValue)
            throw AntiFraudException.CodeAlreadyUsed(code);
    }
}
