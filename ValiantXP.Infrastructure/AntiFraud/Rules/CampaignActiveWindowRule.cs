using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Exceptions;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Order 5 — All dynamic types.
/// Verifies submissions fall within the campaign's active date window.
/// Runs first so subsequent rules are not hit unnecessarily.
/// Config key: AntiFraudCampaignConfig.EnforceCampaignDateWindow
/// </summary>
public sealed class CampaignActiveWindowRule : IAntiFraudRule
{
    public Domain.Enums.DynamicType? ApplicableType => null; // applies to all
    public int Order => 5;

    public Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        if (!context.Config.EnforceCampaignDateWindow) return Task.CompletedTask;

        var now = DateTime.UtcNow;
        if (now < context.CampaignStartDate || now > context.CampaignEndDate)
            throw AntiFraudException.CampaignNotActive();

        return Task.CompletedTask;
    }
}
