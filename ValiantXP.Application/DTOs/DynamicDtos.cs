using System;
using System.Collections.Generic;

namespace ValiantXP.Application.DTOs;

public class ChallengeDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool AnonParticipationAllowed { get; set; }
    public string ConfigurationJson { get; set; } = string.Empty;
}

public class SubmitChallengeRequestDto
{
    public Dictionary<string, string> Inputs { get; set; } = new();
}

public class ChallengeResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Payload { get; set; }
    public List<string> AwardedPrizeNames { get; set; } = new();
    public string? AwardedPrizeName => AwardedPrizeNames.Count > 0 ? AwardedPrizeNames[0] : null;
    public int PointsAwarded { get; set; }
    public Guid? NextChallengeId { get; set; }
}

