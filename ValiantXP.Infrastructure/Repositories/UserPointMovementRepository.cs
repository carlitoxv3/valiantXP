using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class UserPointMovementRepository : IUserPointMovementRepository
{
    private readonly ApplicationDbContext _context;

    public UserPointMovementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task AddAsync(UserPointMovement movement, CancellationToken ct = default)
    {
        await _context.UserPointMovements.AddAsync(movement, ct);
        // Note: caller is responsible for SaveChangesAsync (via strategy)
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalPointsAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.UserPointMovements
            .Where(m => m.UserId == userId
                     && m.Points > 0
                     && (m.ExpiresAt == null || m.ExpiresAt > now))
            .SumAsync(m => m.Points, ct);
    }
}
