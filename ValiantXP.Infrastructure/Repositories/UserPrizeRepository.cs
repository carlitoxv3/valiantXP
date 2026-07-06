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

public class UserPrizeRepository : GenericRepository<UserPrize>, IUserPrizeRepository
{
    public UserPrizeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserPrize>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(up => up.Prize)
            .Where(up => up.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetAwardCountInWindowAsync(Guid prizeId, DateTime windowStart, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(up => up.PrizeId == prizeId && up.AwardedAt >= windowStart)
            .CountAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<int> GetUserAwardCountInWindowAsync(Guid userId, Guid prizeId, DateTime windowStart, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(up => up.UserId == userId && up.PrizeId == prizeId && up.AwardedAt >= windowStart)
            .CountAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<bool> UserAlreadyWonAsync(Guid userId, Guid prizeId, CancellationToken ct = default)
    {
        return await _dbSet
            .AnyAsync(up => up.UserId == userId && up.PrizeId == prizeId, ct);
    }
}
