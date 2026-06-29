using System;
using ValiantXP.Domain.Common;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Entities;

public class UserChallengeProgress : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid DynamicChallengeId { get; set; }
    public DynamicChallenge DynamicChallenge { get; set; } = null!;

    public ChallengeStatus Status { get; set; }
    public int Attempts { get; set; }
    public int Score { get; set; }
    public DateTime? CompletedAt { get; set; }
}
