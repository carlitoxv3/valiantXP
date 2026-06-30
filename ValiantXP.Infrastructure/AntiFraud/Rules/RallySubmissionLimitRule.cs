using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Enforces the per-user submission frequency limit for Rally dynamics.
/// Mirrors PromoHub's RallyMultimediaMaxUploadDaily validator:
///   config.Rally.MaxSubmissionsPerUserPerPeriod within config.Rally.PeriodHours.
///
/// Order 30 — runs after campaign date window (10) and before ticket uniqueness (40).
/// </summary>
public sealed class RallySubmissionLimitRule : IAntiFraudRule
{
    private readonly IRallySubmissionRepository _submissionRepo;

    public DynamicType? ApplicableType => DynamicType.Rally;
    public int Order => 30;

    public RallySubmissionLimitRule(IRallySubmissionRepository submissionRepo)
    {
        _submissionRepo = submissionRepo;
    }

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        var cfg = context.Config.Rally;
        if (cfg.MaxSubmissionsPerUserPerPeriod <= 0) return; // 0 = unlimited

        var windowStart = DateTime.UtcNow.AddHours(-cfg.PeriodHours);

        var count = await _submissionRepo.GetSubmissionCountAsync(
            context.UserId, context.ChallengeId, windowStart, cancellationToken);

        if (count >= cfg.MaxSubmissionsPerUserPerPeriod)
        {
            throw AntiFraudException.RallySubmissionLimitExceeded(
                cfg.MaxSubmissionsPerUserPerPeriod,
                cfg.PeriodHours);
        }
    }
}
