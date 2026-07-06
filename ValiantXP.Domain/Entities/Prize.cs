using System;
using System.Collections.Generic;
using ValiantXP.Domain.Common;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Entities;

public class Prize : Entity
{
    public Guid DynamicChallengeId { get; set; }
    public DynamicChallenge DynamicChallenge { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int RemainingQuantity { get; set; }
    public string Type { get; set; } = string.Empty; // legacy — use PrizeType instead

    // --- InstantWin extension fields ---
    /// <summary>Typed prize category (Points / Product / GiftCard / WheelChance).</summary>
    public PrizeType PrizeType { get; set; } = PrizeType.Points;

    /// <summary>Mirrors PromoHub Coupon.AllowsNotWin — allows a null (no-win) slot in random selection.</summary>
    public bool AllowNoWin { get; set; } = false;

    /// <summary>Time window in hours for global/per-user award limits. 0 = no window (unlimited).</summary>
    public int WindowHours { get; set; } = 0;

    /// <summary>Max awards globally within WindowHours. 0 = unlimited. Maps to CouponDetail.QuantityToUse.</summary>
    public int MaxGlobalInWindow { get; set; } = 0;

    /// <summary>Max awards per user within WindowHours. 0 = unlimited. Maps to CouponDetail.QuantityToUsePerUser.</summary>
    public int MaxPerUserInWindow { get; set; } = 0;

    /// <summary>If > 0: final points = PointMultiplier × user's total points. Maps to CouponDetail.Multiplier.</summary>
    public int PointMultiplier { get; set; } = 0;

    /// <summary>Days before awarded points expire. 0 = no expiry. Maps to Configuration.ModuleTrivias_DaysOff.</summary>
    public int PointsExpirationDays { get; set; } = 0;

    /// <summary>External GiftCard provider reference. Maps to Product.GiftCardProviderId.</summary>
    public string? ExternalReference { get; set; }

    /// <summary>Human-readable description of the prize.</summary>
    public string? Description { get; set; }

    /// <summary>URL of prize image.</summary>
    public string? ImageUrl { get; set; }

    public ICollection<UserPrize> UserPrizes { get; set; } = new List<UserPrize>();
}
