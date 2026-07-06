using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class UserIdentityRepository : IUserIdentityRepository
{
    private readonly ApplicationDbContext _context;

    public UserIdentityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserIdentity?> FindAsync(IdentityProvider provider, string externalId, CancellationToken ct = default)
        => await _context.UserIdentities
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ExternalId == externalId && x.IsActive, ct);

    public async Task<IReadOnlyList<UserIdentity>> FindByEmailClaimAsync(string emailClaim, bool onlyVerified = true, CancellationToken ct = default)
    {
        var query = _context.UserIdentities
            .Where(x => x.EmailClaim == emailClaim && x.IsActive);
        if (onlyVerified)
            query = query.Where(x => x.IsEmailVerified);
        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserIdentity>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.UserIdentities
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.LinkedAt)
            .ToListAsync(ct);

    public async Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default)
        => await _context.UserIdentities
            .CountAsync(x => x.UserId == userId && x.IsActive, ct);

    public async Task AddAsync(UserIdentity identity, CancellationToken ct = default)
        => await _context.UserIdentities.AddAsync(identity, ct);

    public Task UpdateAsync(UserIdentity identity, CancellationToken ct = default)
    {
        _context.UserIdentities.Update(identity);
        return Task.CompletedTask;
    }
}
