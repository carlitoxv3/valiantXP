using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
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
}
