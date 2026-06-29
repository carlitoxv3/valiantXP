using System;
using System.Collections.Generic;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.AntiFraud;

/// <summary>
/// Immutable context passed to every IAntiFraudRule during pipeline execution.
/// Built once per request by SubmitChallengeCommandHandler to avoid per-rule DB queries.
/// </summary>
public sealed class AntiFraudContext
{
    public Guid UserId { get; init; }
    public Guid ChallengeId { get; init; }
    public Guid CampaignId { get; init; }
    public DynamicType ChallengeType { get; init; }

    /// <summary>Remote IP address of the incoming request. Null in unit-test contexts.</summary>
    public string? RemoteIp { get; init; }

    /// <summary>Raw user inputs (same map used by IDynamicStrategy). Codigo: key = "code".</summary>
    public IReadOnlyDictionary<string, string> Inputs { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Shorthand for Inputs["code"] (trimmed). Null for Trivia/Encuesta dynamics.
    /// </summary>
    public string? SubmittedCode =>
        Inputs.TryGetValue("code", out var v) ? v?.Trim() : null;

    /// <summary>
    /// Full campaign-level anti-fraud config, deserialized from DynamicChallenge.AntiFraudConfigJson.
    /// Never null — a default config (with sensible limits) is used when none is stored.
    /// Access module-specific config via Config.Codigo, Config.Trivia, Config.Encuesta.
    /// </summary>
    public AntiFraudCampaignConfig Config { get; init; } = new();

    /// <summary>Campaign active window — used by CampaignActiveWindowRule.</summary>
    public DateTime CampaignStartDate { get; init; }
    public DateTime CampaignEndDate { get; init; }
}
