using System;
using ValiantXP.Domain.Common;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Entities;

public class UserPrize : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PrizeId { get; set; }
    public Prize Prize { get; set; } = null!;

    public DateTime AwardedAt { get; set; }
    public string Code { get; set; } = string.Empty;

    // --- InstantWin extension fields ---
    /// <summary>Snapshot of the prize type at award time.</summary>
    public PrizeType PrizeType { get; set; } = PrizeType.Points;

    /// <summary>Actual points credited for Points-type prizes.</summary>
    public int PointsAwarded { get; set; } = 0;

    /// <summary>GiftCard code for GiftCard-type prizes (internal or external).</summary>
    public string? GiftCardCode { get; set; }

    /// <summary>Expiration date for points, null if no expiry.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Rally submission that triggered this award.</summary>
    public Guid? SubmissionId { get; set; }

    /// <summary>Whether the prize has been redeemed by the user.</summary>
    public bool IsRedeemed { get; set; } = false;

    /// <summary>When the prize was redeemed.</summary>
    public DateTime? RedeemedAt { get; set; }
}
