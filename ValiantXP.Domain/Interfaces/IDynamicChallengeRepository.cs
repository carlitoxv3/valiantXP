using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IDynamicChallengeRepository : IRepository<DynamicChallenge>
{
    Task<DynamicChallenge?> GetWithPrizesAsync(Guid id, CancellationToken cancellationToken = default);
}
