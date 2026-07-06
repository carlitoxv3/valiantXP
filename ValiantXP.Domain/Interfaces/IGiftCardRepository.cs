using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IGiftCardRepository
{
    /// <summary>
    /// Atomically assigns the next available gift card from the pool to a user.
    /// Uses UPDATE TOP(1) ... OUTPUT to prevent race conditions.
    /// Returns null if the pool is empty.
    /// </summary>
    Task<GiftCard?> TryAssignFromPoolAsync(
        Guid providerId,
        Guid userId,
        Guid userPrizeId,
        CancellationToken ct = default);

    /// <summary>Count of available (unassigned) codes in the pool for a provider.</summary>
    Task<int> GetAvailableCountAsync(Guid providerId, CancellationToken ct = default);

    /// <summary>Bulk insert codes for a provider (import from CSV).</summary>
    Task BulkInsertAsync(IEnumerable<GiftCard> cards, CancellationToken ct = default);

    /// <summary>Check if a code already exists for a provider (duplicate detection).</summary>
    Task<bool> CodeExistsAsync(Guid providerId, string code, CancellationToken ct = default);
}
