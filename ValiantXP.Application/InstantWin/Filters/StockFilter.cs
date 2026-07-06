using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Filters;

/// <summary>
/// Blocks prizes with RemainingQuantity == 0.
/// Passes when RemainingQuantity > 0 (stock available).
/// Maps to the "Stock IS NULL OR Stock > 0" check in dbo.InstantWin_Calculate.
/// </summary>
public class StockFilter : IPrizeFilter
{
    public Task<bool> IsEligibleAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct = default)
    {
        // If RemainingQuantity is 0, stock is depleted → not eligible
        // Positive = has stock, future: negative could mean unlimited
        var eligible = prize.RemainingQuantity > 0;
        return Task.FromResult(eligible);
    }
}
