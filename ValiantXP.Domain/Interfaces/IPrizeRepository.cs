using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IPrizeRepository : IRepository<Prize>
{
    Task<IEnumerable<Prize>> GetByChallengeIdAsync(Guid challengeId, CancellationToken cancellationToken = default);
}
