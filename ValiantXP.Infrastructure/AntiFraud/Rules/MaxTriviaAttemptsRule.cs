using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Order 30 — Trivia only.
/// Limits the number of submission attempts a user may make on a trivia challenge.
/// Config key: AntiFraudCampaignConfig.Trivia.MaxAttemptsPerUser
/// </summary>
public sealed class MaxTriviaAttemptsRule : IAntiFraudRule
{
    private readonly ApplicationDbContext _context;
    public DynamicType? ApplicableType => DynamicType.Trivia;
    public int Order => 30;

    public MaxTriviaAttemptsRule(ApplicationDbContext context) => _context = context;

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        var limit = context.Config.Trivia.MaxAttemptsPerUser;
        if (limit <= 0) return; // 0 = unlimited

        var attemptCount = await _context.UserChallengeProgresses
            .CountAsync(p =>
                p.UserId == context.UserId &&
                p.DynamicChallengeId == context.ChallengeId,
                cancellationToken);

        if (attemptCount >= limit)
            throw AntiFraudException.TriviaAttemptsExceeded(limit);
    }
}
