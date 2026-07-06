using System;
using System.Collections.Generic;
using System.Linq;

namespace ValiantXP.Application.Models;

/// <summary>
/// Configuration for position-based instant-win on any DynamicChallenge.
/// Stored as JSON in DynamicChallenge.PositionWinConfigJson.
/// </summary>
public class PositionWinConfig
{
    public bool Enabled { get; set; }

    /// <summary>If true, position counter resets daily (UTC). If false, counts all time.</summary>
    public bool DailyReset { get; set; } = true;

    /// <summary>Tiers used when no schedule matches the current date.</summary>
    public List<PositionTier> DefaultTiers { get; set; } = new();

    /// <summary>Date-range overrides. First matching schedule wins.</summary>
    public List<PositionSchedule> Schedules { get; set; } = new();

    /// <summary>Returns the active tiers for the given UTC datetime.</summary>
    public List<PositionTier> GetActiveTiers(DateTime utcNow)
    {
        var schedule = Schedules.FirstOrDefault(s => utcNow >= s.From && utcNow <= s.To);
        return schedule?.Tiers ?? DefaultTiers;
    }

    /// <summary>
    /// Returns the tier matching the given position from the active tiers, or null if not a winner.
    /// </summary>
    public PositionTier? GetWinningTier(int position, DateTime utcNow)
    {
        var tiers = GetActiveTiers(utcNow);
        return tiers.FirstOrDefault(t => t.Positions.Contains(position));
    }
}

public class PositionSchedule
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<PositionTier> Tiers { get; set; } = new();
}

public class PositionTier
{
    public List<int> Positions { get; set; } = new();

    /// <summary>Maps to Prize.ExternalReference for prize selection.</summary>
    public string PrizeTag { get; set; } = string.Empty;
}
