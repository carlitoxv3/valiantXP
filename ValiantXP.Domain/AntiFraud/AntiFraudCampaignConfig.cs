namespace ValiantXP.Domain.AntiFraud;

/// <summary>
/// Per-campaign anti-fraud configuration stored as JSON in DynamicChallenge.AntiFraudConfigJson.
/// Each module (Code, Trivia, Survey) has its own nested config section.
/// A null module config means the module uses its hardcoded defaults.
/// </summary>
public class AntiFraudCampaignConfig
{
    // ─── Cross-module rules (apply to all dynamics) ───────────────────────────

    /// <summary>
    /// Enforces that submissions are only accepted between Campaign.StartDate and Campaign.EndDate.
    /// Disable only for internal testing scenarios.
    /// </summary>
    public bool EnforceCampaignDateWindow { get; set; } = true;

    // ─── Module-specific configs ──────────────────────────────────────────────

    /// <summary>Anti-fraud rules specific to the Code (promo code) dynamic.</summary>
    public CodeAntiFraudConfig Code { get; set; } = new();

    /// <summary>Anti-fraud rules specific to the Trivia dynamic.</summary>
    public TriviaAntiFraudConfig Trivia { get; set; } = new();

    /// <summary>Anti-fraud rules specific to the Survey dynamic.</summary>
    public SurveyAntiFraudConfig Survey { get; set; } = new();
}

/// <summary>
/// Anti-fraud configuration for the Code (promo code redemption) dynamic.
/// Mirrors the validations performed by PromoHub's dbo.ExchangeCode and
/// dbo.ValidateExchangeCode stored procedures.
/// </summary>
public class CodeAntiFraudConfig
{
    // ── Redemption limits ──

    /// <summary>
    /// Maximum successful code redemptions a single user may perform per calendar day (UTC).
    /// Equivalent to PromoHub's @times parameter in ValidateExchangeCode SP.
    /// </summary>
    public int MaxRedemptionsPerUserPerDay { get; set; } = 1;

    /// <summary>
    /// Maximum code redemption attempts (including failures) from the same IP per hour.
    /// Equivalent to PromoHub's IP-based validation in ValidateExchangeCode SP.
    /// </summary>
    public int MaxAttemptsPerIpPerHour { get; set; } = 5;

    // ── Failed-attempt / bot detection ──

    /// <summary>
    /// Whether to persist each failed attempt to the FailedAttempts table.
    /// Equivalent to PromoHub's FailedAttemptService.RegisterFailedAttempt().
    /// </summary>
    public bool TrackFailedAttempts { get; set; } = true;

    /// <summary>
    /// Number of failed attempts within FailedAttemptWindowMinutes before the user is blocked.
    /// Equivalent to PromoHub's dbo.DetectBots SP parameter nbrFails.
    /// </summary>
    public int MaxFailedAttemptsBeforeBlock { get; set; } = 3;

    /// <summary>
    /// Rolling time window (minutes) in which MaxFailedAttemptsBeforeBlock is evaluated.
    /// Equivalent to PromoHub's dbo.DetectBots SP parameter period.
    /// </summary>
    public int FailedAttemptWindowMinutes { get; set; } = 60;

    // ── External provider ──

    /// <summary>
    /// Optional external validator ID (e.g. "Interalia", "CustomApi").
    /// When set, the ExternalProviderRule resolves the correct validator via DI.
    /// Null = no external validation required (standard PromoHub behaviour).
    /// Equivalent to KOCodeExchangeHandler.PreValidate → _interaliaService.Validate().
    /// </summary>
    public string? ExternalValidatorId { get; set; }
}

/// <summary>
/// Anti-fraud configuration for the Trivia dynamic.
/// </summary>
public class TriviaAntiFraudConfig
{
    /// <summary>
    /// Maximum number of submission attempts per user for this trivia challenge.
    /// 0 means unlimited (useful for practice trivias).
    /// </summary>
    public int MaxAttemptsPerUser { get; set; } = 3;

    /// <summary>
    /// Minimum seconds a user must wait between trivia attempts.
    /// Prevents rapid-fire automated submissions. 0 = no cooldown.
    /// </summary>
    public int CooldownBetweenAttemptsSeconds { get; set; } = 0;

    /// <summary>
    /// Whether to track IP-based attempt rate for this trivia.
    /// Useful for public (unauthenticated) trivias.
    /// </summary>
    public bool TrackIpAttempts { get; set; } = false;

    /// <summary>Max trivia attempts from the same IP per hour when TrackIpAttempts is true.</summary>
    public int MaxIpAttemptsPerHour { get; set; } = 10;
}

/// <summary>
/// Anti-fraud configuration for the Survey dynamic.
/// </summary>
public class SurveyAntiFraudConfig
{
    /// <summary>
    /// Whether a user can only submit this survey once.
    /// True by default — surveys are typically one-time events.
    /// </summary>
    public bool OncePerUser { get; set; } = true;

    /// <summary>
    /// Whether to enforce IP uniqueness for the survey (one submission per IP).
    /// Useful for anonymous surveys where user identity is not guaranteed.
    /// </summary>
    public bool EnforceIpUniqueness { get; set; } = false;

    /// <summary>Max survey submissions from the same IP when EnforceIpUniqueness is true.</summary>
    public int MaxSubmissionsPerIp { get; set; } = 1;
}
