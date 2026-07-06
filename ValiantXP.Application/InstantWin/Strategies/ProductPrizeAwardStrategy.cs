using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Strategies;

/// <summary>
/// Awards a Product prize (including GiftCard-delivered products):
///   1. Atomically decrements RemainingQuantity via raw SQL
///   2. Throws if stock is already depleted (handles race condition)
///   3. IF prize.GiftCardProviderId != null → assigns a code from the GiftCard pool
///      (throws GiftCardOutOfStockException if pool is empty)
///   4. ELSE → generates an internal "VXP-PROD-XXXXXX" code
///   5. Creates UserPrize with PrizeType.Product (and GiftCardCode if applicable)
/// Maps to the Product branch of dbo.InstantWin_Save + dbo.SetGiftCard in PromoHub.
/// </summary>
public class ProductPrizeAwardStrategy : IPrizeAwardStrategy
{
    private readonly IApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public ProductPrizeAwardStrategy(IApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public bool CanHandle(PrizeType prizeType) => prizeType == PrizeType.Product;

    public async Task<UserPrize> AwardAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct)
    {
        // 1. Atomic stock decrement — raw SQL prevents race conditions
        bool decremented = await _db.TryDecrementPrizeStockAsync(prize.Id, ct);

        if (!decremented)
            throw new InvalidOperationException(
                $"Prize {prize.Id} ('{prize.Name}') stock depleted — concurrent award race condition.");

        // 2. Generate unique award code
        var code = $"VXP-PROD-{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        // 3. Create UserPrize record (GiftCardCode will be set below if applicable)
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

        // 4. If prize uses a GiftCard provider pool, atomically assign a code from it.
        //    This replicates PromoHub's dbo.SetGiftCard SP logic (UPDATE TOP(1)...OUTPUT).
        if (prize.GiftCardProviderId.HasValue)
        {
            var giftCard = await _uow.GiftCards.TryAssignFromPoolAsync(
                prize.GiftCardProviderId.Value,
                context.UserId,
                userPrize.Id,
                ct);

            if (giftCard is null)
                throw new GiftCardOutOfStockException(prize.GiftCardProviderId.Value);

            userPrize.GiftCardCode = giftCard.Code;
        }

        await _db.UserPrizes.AddAsync(userPrize, ct);
        await _db.SaveChangesAsync(ct);

        return userPrize;
    }
}
