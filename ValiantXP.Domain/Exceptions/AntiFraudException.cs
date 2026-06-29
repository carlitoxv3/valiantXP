using System;

namespace ValiantXP.Domain.Exceptions;

/// <summary>
/// Thrown by any IAntiFraudRule when a submission is rejected due to fraud signals.
/// Maps to HTTP 422 (Unprocessable Entity) or 429 (Too Many Requests) in the API layer.
/// </summary>
public sealed class AntiFraudException : Exception
{
    /// <summary>Machine-readable code identifying the specific rule that fired.</summary>
    public string RuleCode { get; }

    public AntiFraudException(string ruleCode, string message)
        : base(message)
    {
        RuleCode = ruleCode;
    }

    // Common factory methods for consistent messaging

    public static AntiFraudException CodeNotFound(string code) =>
        new("CODE_NOT_FOUND", $"The code '{code}' does not exist or is not valid for this campaign.");

    public static AntiFraudException CodeAlreadyUsed(string code) =>
        new("CODE_ALREADY_USED", $"The code '{code}' has already been redeemed.");

    public static AntiFraudException DailyLimitExceeded(int limit) =>
        new("DAILY_LIMIT_EXCEEDED", $"You have reached the maximum of {limit} redemption(s) allowed per day.");

    public static AntiFraudException IpLimitExceeded(int limit) =>
        new("IP_LIMIT_EXCEEDED", $"Too many requests from this IP address. Limit: {limit} per hour.");

    public static AntiFraudException UserBlocked(int failedAttempts, int windowMinutes) =>
        new("USER_BLOCKED", $"Your account has been temporarily blocked after {failedAttempts} failed attempts within {windowMinutes} minutes. Please try again later.");

    public static AntiFraudException TriviaAttemptsExceeded(int limit) =>
        new("TRIVIA_ATTEMPTS_EXCEEDED", $"You have reached the maximum of {limit} attempt(s) for this trivia.");

    public static AntiFraudException SurveyAlreadyAnswered() =>
        new("SURVEY_ALREADY_ANSWERED", "You have already completed this survey.");

    public static AntiFraudException CampaignNotActive() =>
        new("CAMPAIGN_NOT_ACTIVE", "This campaign is not currently active.");
}
