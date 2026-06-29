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
    public string ConfigurationJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? NextChallengeId { get; set; }

    /// <summary>
    /// JSON blob deserialized to AntiFraudCampaignConfig at runtime.
    /// Null means all modules use their default limits.
    /// Each root key maps to a module section: { "Codigo": {...}, "Trivia": {...}, "Encuesta": {...} }
    /// </summary>
    public string? AntiFraudConfigJson { get; set; }

    public ICollection<Prize> Prizes { get; set; } = new List<Prize>();
    public ICollection<UserChallengeProgress> UserProgresses { get; set; } = new List<UserChallengeProgress>();
}
