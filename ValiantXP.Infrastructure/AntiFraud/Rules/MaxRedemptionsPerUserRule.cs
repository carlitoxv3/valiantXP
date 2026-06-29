using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Order 30 — Codigo only.
/// Limits the number of successful redemptions a single user may perform per calendar day (UTC).
/// Equivalent to PromoHub's dbo.ValidateExchangeCode SP user-limit check.
/// Config key: AntiFraudCampaignConfig.Codigo.MaxRedemptionsPerUserPerDay
/// </summary>
public sealed class MaxRedemptionsPerUserRule : IAntiFraudRule
{
    private readonly ApplicationDbContext _context;
    public DynamicType? ApplicableType => DynamicType.Code;
    public int Order => 30;

    public MaxRedemptionsPerUserRule(ApplicationDbContext context) => _context = context;

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        var limit = context.Config.Code.MaxRedemptionsPerUserPerDay;
        if (limit <= 0) return; // 0 = unlimited

        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        // Count codes the user successfully redeemed today (UsedAt populated = redeemed)
        var count = await _context.Codes
            .CountAsync(c =>
                c.UserId == context.UserId &&
                c.UsedAt.HasValue &&
                c.UsedAt.Value >= todayUtc &&
                c.UsedAt.Value < tomorrowUtc,
                cancellationToken);

        if (count >= limit)
            throw AntiFraudException.DailyLimitExceeded(limit);
    }
}
