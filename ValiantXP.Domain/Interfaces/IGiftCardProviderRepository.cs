using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IGiftCardProviderRepository
{
    Task<GiftCardProvider?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<GiftCardProvider>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GiftCardProvider>> GetByCampaignAsync(Guid campaignId, CancellationToken ct = default);
    Task AddAsync(GiftCardProvider provider, CancellationToken ct = default);
    Task UpdateAsync(GiftCardProvider provider, CancellationToken ct = default);
}
