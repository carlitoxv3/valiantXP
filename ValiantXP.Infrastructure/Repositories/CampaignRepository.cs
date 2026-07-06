using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class CampaignRepository : GenericRepository<Campaign>, ICampaignRepository
{
    private readonly ApplicationDbContext _db;

    public CampaignRepository(ApplicationDbContext context) : base(context)
    {
        _db = context;
    }

    public async Task<IReadOnlyList<Campaign>> GetAllWithChallengesAsync(CancellationToken ct = default) =>
        await _db.Campaigns
            .Include(c => c.Challenges)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<Campaign?> GetWithChallengesAsync(Guid id, CancellationToken ct = default) =>
        await _db.Campaigns
            .Include(c => c.Challenges)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}

