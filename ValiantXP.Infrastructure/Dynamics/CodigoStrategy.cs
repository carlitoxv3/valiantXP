using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Dynamics;

/// <summary>
/// Strategy for the Code (promo code redemption) dynamic.
/// Validates the submitted code exists and has not been redeemed,
/// then marks it as used atomically. Prize assignment is delegated
/// to ChallengeCompletedEvent (PromoHub pattern).
///
/// Position-based win evaluation has been moved to the universal
/// IPositionWinService, called by SubmitChallengeCommandHandler
/// for ALL challenge types. This strategy is now a clean code validator.
/// </summary>
public class CodeStrategy : IDynamicStrategy
{
    private readonly IUnitOfWork _unitOfWork;

    public CodeStrategy(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public string DynamicType => Domain.Enums.DynamicType.Code.ToString();

    public async Task<DynamicResult> ExecuteAsync(DynamicContext context, CancellationToken cancellationToken)
    {
        if (!context.Inputs.TryGetValue("code", out var submittedCode) || string.IsNullOrWhiteSpace(submittedCode))
        {
            return new DynamicResult
            {
                Success = false,
                Message = "No code was submitted. Please provide a 'code' input."
            };
        }

        var code = await _unitOfWork.Codes.GetByCodeNumberAsync(submittedCode.Trim(), cancellationToken);

        if (code == null)
        {
            return new DynamicResult
            {
                Success = false,
                Message = "The provided code does not exist or is not valid for this campaign."
            };
        }

        if (code.UsedAt.HasValue)
        {
            return new DynamicResult
            {
                Success = false,
                Message = "This code has already been redeemed."
            };
        }

        // Mark code as used (PromoHub pattern: exchange atomically then award prize via event)
        code.UsedAt = DateTime.UtcNow;
        code.UserId = context.UserId;
        await _unitOfWork.Codes.UpdateAsync(code, cancellationToken);

        // Standard code redemption — position win evaluation handled universally
        // by IPositionWinService in SubmitChallengeCommandHandler.
        return new DynamicResult
        {
            Success = true,
            Message = $"Code '{submittedCode.Trim()}' successfully redeemed!",
            Payload = new Dictionary<string, object>
            {
                { "CodeNumber", code.CodeNumber },
                { "CampaignId", code.CampaignId }
            }
        };
    }
}
