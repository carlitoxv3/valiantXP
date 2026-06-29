using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IUserPrizeRepository : IRepository<UserPrize>
{
    Task<IEnumerable<UserPrize>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
