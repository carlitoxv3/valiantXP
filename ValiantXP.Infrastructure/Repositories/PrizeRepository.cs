using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class PrizeRepository : GenericRepository<Prize>, IPrizeRepository
{
    public PrizeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Prize>> GetByChallengeIdAsync(Guid challengeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.DynamicChallengeId == challengeId)
            .ToListAsync(cancellationToken);
    }
}
