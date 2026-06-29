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
}
