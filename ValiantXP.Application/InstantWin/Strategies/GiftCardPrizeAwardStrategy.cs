using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Strategies;

/// <summary>
/// Awards a GiftCard prize:
///   1. Atomically decrements stock
///   2. Issues GiftCard code via external provider if ExternalReference is set,
///      otherwise generates an internal "VXP-GC-XXXXXX" code
///   3. Creates UserPrize with GiftCardCode
/// Maps to the GiftCard branch + dbo.SetGiftCard call in dbo.InstantWin_Save.
/// </summary>
/// <remarks>
/// OBSOLETE — Sprint 10 (GC-4): This strategy has been ABSORBED by <see cref="ProductPrizeAwardStrategy"/>.
/// GiftCard delivery is no longer a separate PrizeType; it is a delivery mechanism for PrizeType.Product
/// controlled by <c>Prize.GiftCardProviderId != null</c>.
/// The synthetic "VXP-GC-GUID" fallback code generation here is NOT a real GiftCard code.
/// DO NOT remove this file yet — Infrastructure must update <c>InstantWinAwarder</c> routing
/// (currently dispatches by enum value PrizeType.GiftCard) before this class can be deleted.
/// Once InstantWinAwarder routes by GiftCardProviderId instead of enum, this class will be safely unused.
/// Tracked in Sprint 10 GC-5 (PrizeType.GiftCard removal).
/// </remarks>
[Obsolete("Use PrizeType.Product + Prize.GiftCardProviderId != null. Absorbed by ProductPrizeAwardStrategy. Remove after InstantWinAwarder is updated in Sprint 10 (GC-5).")]
public class GiftCardPrizeAwardStrategy : IPrizeAwardStrategy
{
    private readonly IApplicationDbContext _db;
    private readonly IEnumerable<IGiftCardProvider> _providers;

    public GiftCardPrizeAwardStrategy(IApplicationDbContext db, IEnumerable<IGiftCardProvider> providers)
    {
        _db = db;
        _providers = providers;
    }

    // OBSOLETE: PrizeType.GiftCard has been removed. CanHandle intentionally returns false.
    // GiftCard delivery is now handled by ProductPrizeAwardStrategy via Prize.GiftCardProviderId.
    public bool CanHandle(PrizeType prizeType) => false;

    public async Task<UserPrize> AwardAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct)
    {
        // 1. Atomic stock decrement
        bool decremented = await _db.TryDecrementPrizeStockAsync(prize.Id, ct);

        if (!decremented)
            throw new InvalidOperationException(
                $"GiftCard prize {prize.Id} ('{prize.Name}') stock depleted.");

        string? giftCardCode = null;

        // 2. Issue GiftCard code
        if (prize.ExternalReference != null)
        {
            var provider = _providers.FirstOrDefault(p =>
                p.ProviderName.Equals(prize.ExternalReference, StringComparison.OrdinalIgnoreCase));

            if (provider != null)
            {
                var result = await provider.IssueGiftCardAsync(context.UserId, prize.Id, ct);
                giftCardCode = result.Success ? result.Code : null;
            }
        }

        // Fallback: internal code generation
        giftCardCode ??= $"VXP-GC-{Guid.NewGuid():N}"[..14].ToUpperInvariant();

        // 3. Generate unique award code (different from the GiftCard code itself)
        var code = $"VXP-GCA-{Guid.NewGuid():N}"[..15].ToUpperInvariant();

        var userPrize = new UserPrize
        {
            Id = Guid.NewGuid(),
            UserId = context.UserId,
            PrizeId = prize.Id,
            PrizeType = PrizeType.Product, // Was PrizeType.GiftCard — removed in Sprint 10 (GC-5)
            GiftCardCode = giftCardCode,
            Code = code,
            AwardedAt = context.Now,
            SubmissionId = context.SubmissionId
        };

        await _db.UserPrizes.AddAsync(userPrize, ct);
        await _db.SaveChangesAsync(ct);

        return userPrize;
    }
}
