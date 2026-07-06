using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

/// <summary>Result of evaluating position-based win conditions for a challenge submission.</summary>
public record PositionWinResult(
    bool IsEnabled,
    int Position,
    bool IsWinner,
    string? PrizeTier,
    Guid? ReservedPrizeId);

/// <summary>
/// Evaluates whether the current submission wins a positional prize.
/// Position is isolated per DynamicChallengeId (Code, Trivia, Survey, Rally each count independently).
/// </summary>
public interface IPositionWinService
{
    /// <summary>
    /// Evaluates position win for a challenge submission.
    /// </summary>
    /// <param name="challengeId">The specific DynamicChallenge.Id — position count is isolated to this ID.</param>
    /// <param name="positionWinConfigJson">JSON from DynamicChallenge.PositionWinConfigJson. Null = disabled.</param>
    /// <param name="prizes">All prizes for this challenge (to select IsPositionalReward prize by tier).</param>
    Task<PositionWinResult> EvaluateAsync(
        Guid challengeId,
        string? positionWinConfigJson,
        IReadOnlyList<Prize> prizes,
        CancellationToken ct = default);
}
