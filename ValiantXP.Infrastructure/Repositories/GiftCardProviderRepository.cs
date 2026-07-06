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

public class GiftCardProviderRepository : GenericRepository<GiftCardProvider>, IGiftCardProviderRepository
{
    private readonly ApplicationDbContext _db;

    public GiftCardProviderRepository(ApplicationDbContext context) : base(context)
    {
        _db = context;
    }

    public async Task<GiftCardProvider?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<GiftCardProvider>> GetAllActiveAsync(CancellationToken ct = default)
        => await _dbSet.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<GiftCardProvider>> GetByCampaignAsync(Guid campaignId, CancellationToken ct = default)
        => await _dbSet.Where(x => x.CampaignId == campaignId && x.IsActive).ToListAsync(ct);

    public new async Task AddAsync(GiftCardProvider provider, CancellationToken ct = default)
        => await _dbSet.AddAsync(provider, ct);

    public new Task UpdateAsync(GiftCardProvider provider, CancellationToken ct = default)
    {
        _dbSet.Update(provider);
        return Task.CompletedTask;
    }
}
