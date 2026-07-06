using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class UserChallengeProgressRepository : GenericRepository<UserChallengeProgress>, IUserChallengeProgressRepository
{
    public UserChallengeProgressRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<UserChallengeProgress?> GetByUserAndChallengeAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ucp => ucp.UserId == userId && ucp.DynamicChallengeId == challengeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetDailyCompletionCountAsync(Guid challengeId, DateTime date, CancellationToken ct = default)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return await _dbSet
            .CountAsync(ucp =>
                ucp.DynamicChallengeId == challengeId &&
                ucp.Status == ChallengeStatus.Completed &&
                ucp.CompletedAt >= dayStart &&
                ucp.CompletedAt < dayEnd,
                ct);
    }

    /// <inheritdoc />
    public async Task<UserChallengeProgress?> GetLatestCodeProgressWithReservationAsync(
        Guid userId, Guid campaignId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(ucp => ucp.DynamicChallenge)
            .Where(ucp =>
                ucp.UserId == userId &&
                ucp.DynamicChallenge.CampaignId == campaignId &&
                ucp.DynamicChallenge.Type == DynamicType.Code &&
                ucp.ReservedPrizeId != null)
            .OrderByDescending(ucp => ucp.CompletedAt)
            .FirstOrDefaultAsync(ct);
    }
}
