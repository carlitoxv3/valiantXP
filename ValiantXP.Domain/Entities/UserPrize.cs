using System;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

public class UserPrize : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid PrizeId { get; set; }
    public Prize Prize { get; set; } = null!;

    public DateTime AwardedAt { get; set; }
    public string Code { get; set; } = string.Empty;
}
