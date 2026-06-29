using System;
using System.Collections.Generic;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

public class Prize : Entity
{
    public Guid DynamicChallengeId { get; set; }
    public DynamicChallenge DynamicChallenge { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int RemainingQuantity { get; set; }
    public string Type { get; set; } = string.Empty; // e.g. Coupon, Points

    public ICollection<UserPrize> UserPrizes { get; set; } = new List<UserPrize>();
}
