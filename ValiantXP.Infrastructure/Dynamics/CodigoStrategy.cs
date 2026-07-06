using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
/// Extended: supports "position_based" mode where the daily submission
/// position determines whether the user wins and at which tier.
/// </summary>
public class CodeStrategy : IDynamicStrategy
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserChallengeProgressRepository _progressRepo;

    public CodeStrategy(IUnitOfWork unitOfWork, IUserChallengeProgressRepository progressRepo)
    {
        _unitOfWork = unitOfWork;
        _progressRepo = progressRepo;
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

        // ─── Detect position_based mode ───────────────────────────────────────────
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(context.DynamicId, cancellationToken);
        var configJson = challenge?.ConfigurationJson ?? "{}";

        using var configDoc = JsonDocument.Parse(configJson);
        var configRoot = configDoc.RootElement;

        if (configRoot.TryGetProperty("mode", out var modeProp) && modeProp.GetString() == "position_based")
        {
            // Read winningPositions from config
            int[] winningPositions = Array.Empty<int>();
            if (configRoot.TryGetProperty("winningPositions", out var wpProp))
                winningPositions = wpProp.EnumerateArray().Select(x => x.GetInt32()).ToArray();

            // Count daily completions to determine position
            var today = DateTime.UtcNow.Date;
            var dailyCount = await _progressRepo.GetDailyCompletionCountAsync(context.DynamicId, today, cancellationToken);
            var position = dailyCount + 1; // this submission is the next position

            bool isWinner = winningPositions.Contains(position);

            // Determine tier
            string? prizeTier = null;
            Guid? overrideNextChallengeId = null;
            if (isWinner && configRoot.TryGetProperty("tiers", out var tiersProp))
            {
                foreach (var tier in tiersProp.EnumerateArray())
                {
                    var positions = tier.GetProperty("positions").EnumerateArray().Select(x => x.GetInt32()).ToArray();
                    if (positions.Contains(position))
                    {
                        prizeTier = tier.TryGetProperty("prizeTag", out var pt) ? pt.GetString() : null;
                        if (tier.TryGetProperty("nextChallengeId", out var nc) && Guid.TryParse(nc.GetString(), out var ncGuid))
                            overrideNextChallengeId = ncGuid;
                        break;
                    }
                }
            }

            // Build payload
            var posPayload = new Dictionary<string, object>
            {
                { "Position", position },
                { "IsWinner", isWinner },
                { "DailyCount", dailyCount },
                { "PositionBased", true },
                { "CodeNumber", code.CodeNumber },
                { "CampaignId", code.CampaignId }
            };
            if (prizeTier != null) posPayload["PrizeTier"] = prizeTier;
            if (overrideNextChallengeId.HasValue) posPayload["OverrideNextChallengeId"] = overrideNextChallengeId.Value;

            return new DynamicResult
            {
                Success = true, // code is always valid at this point
                Message = isWinner
                    ? $"¡Eres el visitante #{position} del día! ¡Posición ganadora!"
                    : $"Posición #{position} del día. ¡Sigue intentando!",
                Payload = posPayload
            };
        }
        // ─────────────────────────────────────────────────────────────────────────

        // Standard (non-position_based) code redemption
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
