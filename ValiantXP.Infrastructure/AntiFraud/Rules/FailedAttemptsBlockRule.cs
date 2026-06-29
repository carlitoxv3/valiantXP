using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Order 50 — Codigo only.
/// Blocks users who have exceeded the maximum failed attempts within the configured rolling window.
/// Equivalent to PromoHub's dbo.DetectBots SP.
/// Config keys: AntiFraudCampaignConfig.Codigo.MaxFailedAttemptsBeforeBlock + FailedAttemptWindowMinutes
/// </summary>
public sealed class FailedAttemptsBlockRule : IAntiFraudRule
{
    private readonly ApplicationDbContext _context;
    public DynamicType? ApplicableType => DynamicType.Codigo;
    public int Order => 50;

    public FailedAttemptsBlockRule(ApplicationDbContext context) => _context = context;

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        var cfg = context.Config.Codigo;
        if (!cfg.TrackFailedAttempts || cfg.MaxFailedAttemptsBeforeBlock <= 0) return;

        var windowStart = DateTime.UtcNow.AddMinutes(-cfg.FailedAttemptWindowMinutes);

        var failedCount = await _context.Set<Domain.Entities.FailedAttempt>()
            .CountAsync(f =>
                f.UserId == context.UserId &&
                f.ChallengeId == context.ChallengeId &&
                f.AttemptedAt >= windowStart,
                cancellationToken);

        if (failedCount >= cfg.MaxFailedAttemptsBeforeBlock)
            throw AntiFraudException.UserBlocked(cfg.MaxFailedAttemptsBeforeBlock, cfg.FailedAttemptWindowMinutes);
    }
}
