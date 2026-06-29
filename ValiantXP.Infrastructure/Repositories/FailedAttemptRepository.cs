using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public sealed class FailedAttemptRepository : GenericRepository<FailedAttempt>, IFailedAttemptRepository
{
    public FailedAttemptRepository(ApplicationDbContext context) : base(context) { }

    public async Task<int> CountByUserAsync(Guid userId, Guid challengeId, int windowMinutes, CancellationToken ct = default)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);
        return await _dbSet
            .CountAsync(f =>
                f.UserId == userId &&
                f.ChallengeId == challengeId &&
                f.AttemptedAt >= windowStart,
                ct);
    }

    public async Task<int> CountByIpAsync(string remoteIp, Guid challengeId, int windowMinutes, CancellationToken ct = default)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);
        return await _dbSet
            .CountAsync(f =>
                f.RemoteIp == remoteIp &&
                f.ChallengeId == challengeId &&
                f.AttemptedAt >= windowStart,
                ct);
    }
}
