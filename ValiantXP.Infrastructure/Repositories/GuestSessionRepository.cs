using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class GuestSessionRepository : IGuestSessionRepository
{
    private readonly ApplicationDbContext _context;

    public GuestSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GuestSession?> FindByTokenAsync(string token, CancellationToken ct = default)
        => await _context.GuestSessions
            .FirstOrDefaultAsync(x => x.Token == token, ct);

    public async Task AddAsync(GuestSession session, CancellationToken ct = default)
        => await _context.GuestSessions.AddAsync(session, ct);

    public Task UpdateAsync(GuestSession session, CancellationToken ct = default)
    {
        _context.GuestSessions.Update(session);
        return Task.CompletedTask;
    }

    public async Task ExpireOldSessionsAsync(CancellationToken ct = default)
    {
        // For cleanup jobs — marks expired unconverted sessions
        // Actual cleanup can be a separate scheduled background task
        var expired = await _context.GuestSessions
            .Where(x => x.ExpiresAt < DateTime.UtcNow && !x.ConvertedToUserId.HasValue)
            .ToListAsync(ct);
        // No-op placeholder — hook for background job integration
    }
}
