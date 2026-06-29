using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class DynamicChallengeRepository : GenericRepository<DynamicChallenge>, IDynamicChallengeRepository
{
    public DynamicChallengeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<DynamicChallenge?> GetWithPrizesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(dc => dc.Prizes)
            .FirstOrDefaultAsync(dc => dc.Id == id, cancellationToken);
    }
}
