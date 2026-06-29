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
/// Order 40 — Codigo only.
/// Limits the number of redemption attempts (including failures) from the same IP per hour.
/// Equivalent to PromoHub's dbo.ValidateExchangeCode SP IP-limit check.
/// Config key: AntiFraudCampaignConfig.Codigo.MaxAttemptsPerIpPerHour
/// </summary>
public sealed class MaxAttemptsPerIpRule : IAntiFraudRule
{
    private readonly ApplicationDbContext _context;
    public DynamicType? ApplicableType => DynamicType.Codigo;
    public int Order => 40;

    public MaxAttemptsPerIpRule(ApplicationDbContext context) => _context = context;

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        var limit = context.Config.Codigo.MaxAttemptsPerIpPerHour;
        if (limit <= 0 || string.IsNullOrWhiteSpace(context.RemoteIp)) return;

        var windowStart = DateTime.UtcNow.AddHours(-1);

        // Count all failed attempts from this IP in the last hour for this challenge
        var count = await _context.Set<Domain.Entities.FailedAttempt>()
            .CountAsync(f =>
                f.RemoteIp == context.RemoteIp &&
                f.ChallengeId == context.ChallengeId &&
                f.AttemptedAt >= windowStart,
                cancellationToken);

        if (count >= limit)
            throw AntiFraudException.IpLimitExceeded(limit);
    }
}
