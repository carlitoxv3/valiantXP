using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<DynamicChallenge> DynamicChallenges { get; }
    DbSet<UserChallengeProgress> UserChallengeProgresses { get; }
    DbSet<Prize> Prizes { get; }
    DbSet<UserPrize> UserPrizes { get; }
    DbSet<UserPointMovement> UserPointMovements { get; }
    DbSet<GiftCard> GiftCards { get; }

    /// <summary>Provides access to database infrastructure (raw SQL, migrations, etc.).</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Atomically decrements RemainingQuantity for a prize only if stock > 0.
    /// Returns true if decrement succeeded (1 row affected), false if stock was 0 (race condition).
    /// Executes: UPDATE Prizes SET RemainingQuantity = RemainingQuantity - 1 WHERE Id = @id AND RemainingQuantity > 0
    /// </summary>
    Task<bool> TryDecrementPrizeStockAsync(Guid prizeId, CancellationToken ct = default);
}
