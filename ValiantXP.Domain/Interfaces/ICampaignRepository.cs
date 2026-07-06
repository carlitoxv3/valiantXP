using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface ICampaignRepository : IRepository<Campaign>
{
    Task<IReadOnlyList<Campaign>> GetAllWithChallengesAsync(CancellationToken ct = default);
    Task<Campaign?> GetWithChallengesAsync(Guid id, CancellationToken ct = default);
}

