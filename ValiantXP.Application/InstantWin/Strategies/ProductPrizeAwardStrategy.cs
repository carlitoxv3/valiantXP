using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Strategies;

/// <summary>
/// Awards a Product prize:
///   1. Atomically decrements RemainingQuantity via raw SQL
///   2. Throws if stock is already depleted (handles race condition)
///   3. Creates UserPrize with PrizeType.Product
/// Maps to the Product branch of dbo.InstantWin_Save.
/// </summary>
public class ProductPrizeAwardStrategy : IPrizeAwardStrategy
{
    private readonly IApplicationDbContext _db;

    public ProductPrizeAwardStrategy(IApplicationDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(PrizeType prizeType) => prizeType == PrizeType.Product;

    public async Task<UserPrize> AwardAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct)
    {
        // 1. Atomic stock decrement — raw SQL prevents race conditions
        bool decremented = await _db.TryDecrementPrizeStockAsync(prize.Id, ct);

        if (!decremented)
            throw new InvalidOperationException(
                $"Prize {prize.Id} ('{prize.Name}') stock depleted — concurrent award race condition.");

        // 2. Generate unique code
        var code = $"VXP-PROD-{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        // 3. Create UserPrize record
        var userPrize = new UserPrize
        {
            Id = Guid.NewGuid(),
            UserId = context.UserId,
            PrizeId = prize.Id,
            PrizeType = PrizeType.Product,
            Code = code,
            AwardedAt = context.Now,
            SubmissionId = context.SubmissionId
        };

        await _db.UserPrizes.AddAsync(userPrize, ct);
        await _db.SaveChangesAsync(ct);

        return userPrize;
    }
}
