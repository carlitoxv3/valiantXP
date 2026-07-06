using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ValiantXP.Application.Models;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Services;

public class PositionWinService : IPositionWinService
{
    private readonly IUserChallengeProgressRepository _progressRepo;
    private readonly ILogger<PositionWinService> _logger;

    public PositionWinService(
        IUserChallengeProgressRepository progressRepo,
        ILogger<PositionWinService> logger)
    {
        _progressRepo = progressRepo;
        _logger = logger;
    }

    public async Task<PositionWinResult> EvaluateAsync(
        Guid challengeId,
        string? positionWinConfigJson,
        IReadOnlyList<Prize> prizes,
        CancellationToken ct = default)
    {
        // 1. Parse config
        if (string.IsNullOrWhiteSpace(positionWinConfigJson))
            return new PositionWinResult(false, 0, false, null, null);

        PositionWinConfig config;
        try
        {
            config = JsonSerializer.Deserialize<PositionWinConfig>(
                positionWinConfigJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new PositionWinConfig();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse PositionWinConfigJson for challenge {ChallengeId}", challengeId);
            return new PositionWinResult(false, 0, false, null, null);
        }

        if (!config.Enabled)
            return new PositionWinResult(false, 0, false, null, null);

        var now = DateTime.UtcNow;

        // 2. Count position (isolated per DynamicChallengeId)
        //    Position = count of completed submissions today + 1
        var countDate = config.DailyReset ? now.Date : DateTime.MinValue;
        var dailyCount = await _progressRepo.GetDailyCompletionCountAsync(challengeId, countDate, ct);
        var position = dailyCount + 1;

        _logger.LogInformation(
            "PositionWin: Challenge={ChallengeId} Position={Position} (DailyCount={Count})",
            challengeId, position, dailyCount);

        // 3. Find active tier for current date
        var winningTier = config.GetWinningTier(position, now);

        if (winningTier == null)
        {
            _logger.LogInformation(
                "PositionWin: Challenge={ChallengeId} Position={Position} — not a winning position",
                challengeId, position);
            return new PositionWinResult(true, position, false, null, null);
        }

        _logger.LogInformation(
            "PositionWin: Challenge={ChallengeId} Position={Position} — WINNER tier={Tier}",
            challengeId, position, winningTier.PrizeTag);

        // 4. Find the positional prize for this tier
        var reservedPrize = prizes.FirstOrDefault(p =>
            p.IsPositionalReward &&
            string.Equals(p.ExternalReference, winningTier.PrizeTag, StringComparison.OrdinalIgnoreCase)
            && p.RemainingQuantity > 0);

        if (reservedPrize == null)
        {
            _logger.LogWarning(
                "PositionWin: No available positional prize for tier={Tier} on challenge {ChallengeId}",
                winningTier.PrizeTag, challengeId);
            // Still mark as winner (no prize available, but position was winning)
            return new PositionWinResult(true, position, true, winningTier.PrizeTag, null);
        }

        return new PositionWinResult(true, position, true, winningTier.PrizeTag, reservedPrize.Id);
    }
}
