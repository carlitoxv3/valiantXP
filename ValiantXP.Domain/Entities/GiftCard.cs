using System;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

/// <summary>
/// A single pre-loaded gift card code in a provider pool.
/// IsAvailable = true when AssignedToUserId is null.
/// </summary>
public class GiftCard : Entity
{
    public Guid ProviderId { get; set; }
    public GiftCardProvider Provider { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public string? RedeemUrl { get; set; }
    public string? Pin { get; set; }
    public string? Description { get; set; }

    /// <summary>Null = available in pool. Set = assigned to user.</summary>
    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    /// <summary>The UserPrize record that triggered this assignment.</summary>
    public Guid? UserPrizeId { get; set; }
    public UserPrize? UserPrize { get; set; }

    public DateTime? AssignedAt { get; set; }

    public bool IsAvailable => AssignedToUserId is null;
}
