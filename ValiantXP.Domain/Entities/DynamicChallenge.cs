using System;
using System.Collections.Generic;
using ValiantXP.Domain.Common;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Entities;

public class DynamicChallenge : Entity
{
    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public DynamicType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConfigurationJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool AnonParticipationAllowed { get; set; } = false;
    public Guid? NextChallengeId { get; set; }


    /// <summary>
    /// JSON blob deserialized to AntiFraudCampaignConfig at runtime.
    /// Null means all modules use their default limits.
    /// Each root key maps to a module section: { "Codigo": {...}, "Trivia": {...}, "Encuesta": {...} }
    /// </summary>
    public string? AntiFraudConfigJson { get; set; }

    /// <summary>
    /// JSON blob deserialized to PositionWinConfig at runtime.
    /// Null means position-based instant-win is disabled for this challenge.
    /// Stored as nvarchar(max) — see PositionWinConfig in Application.Models.
    /// </summary>
    public string? PositionWinConfigJson { get; set; }

    public ICollection<Prize> Prizes { get; set; } = new List<Prize>();
    public ICollection<UserChallengeProgress> UserProgresses { get; set; } = new List<UserChallengeProgress>();
}
