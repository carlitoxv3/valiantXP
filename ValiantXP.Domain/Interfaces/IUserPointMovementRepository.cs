using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

/// <summary>
/// Repository for user point movement ledger entries.
/// Mirrors PromoHub's InsertMovements SP + ProfileMovements table queries.
/// </summary>
public interface IUserPointMovementRepository
{
    Task AddAsync(UserPointMovement movement, CancellationToken ct = default);

    /// <summary>
    /// Returns the sum of all non-expired point credits for a user.
    /// Excludes movements where ExpiresAt &lt; now.
    /// </summary>
    Task<int> GetTotalPointsAsync(Guid userId, CancellationToken ct = default);
}
