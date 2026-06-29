using System;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

/// <summary>
/// Records each failed dynamic submission attempt per user/IP.
/// Mirrors PromoHub's FailedAttempts table used by the DetectBots SP.
/// Used by FailedAttemptsBlockRule to detect brute-force or bot activity.
/// </summary>
public class FailedAttempt : Entity
{
    /// <summary>The user who made the attempt. May be null for unauthenticated attempts.</summary>
    public Guid? UserId { get; set; }

    /// <summary>The challenge that was attempted.</summary>
    public Guid ChallengeId { get; set; }

    /// <summary>The campaign the challenge belongs to.</summary>
    public Guid CampaignId { get; set; }

    /// <summary>The submitted value (code string for Codigo, null for others).</summary>
    public string? SubmittedValue { get; set; }

    /// <summary>Remote IP address of the request.</summary>
    public string? RemoteIp { get; set; }

    /// <summary>Machine-readable rule code that rejected the attempt (from AntiFraudException.RuleCode).</summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>Human-readable reason for the failure.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the attempt.</summary>
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}
