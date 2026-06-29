using System;
using System.Collections.Generic;

namespace ValiantXP.Application.DTOs;

public class ChallengeDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
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
    public Guid? NextChallengeId { get; set; }
}
