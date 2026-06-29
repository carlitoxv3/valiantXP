using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Order 30 — Encuesta only.
/// Prevents a user from answering the same survey more than once.
/// Config key: AntiFraudCampaignConfig.Encuesta.OncePerUser
/// </summary>
public sealed class SurveyOncePerUserRule : IAntiFraudRule
{
    private readonly ApplicationDbContext _context;
    public DynamicType? ApplicableType => DynamicType.Survey;
    public int Order => 30;

    public SurveyOncePerUserRule(ApplicationDbContext context) => _context = context;

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        if (!context.Config.Survey.OncePerUser) return;

        var alreadyAnswered = await _context.UserChallengeProgresses
            .AnyAsync(p =>
                p.UserId == context.UserId &&
                p.DynamicChallengeId == context.ChallengeId,
                cancellationToken);

        if (alreadyAnswered)
            throw AntiFraudException.SurveyAlreadyAnswered();
    }
}
