using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin;

/// <summary>
/// Implementation of IInstantWinEngine.
/// Mirrors dbo.InstantWin_Calculate:
///   1. Run all IPrizeFilter checks against each prize
///   2. If AllowNoWin=true on any prize, add a null slot to the pool
///   3. Randomize with GUID shuffle and pick first
///   4. Return Prize? (null = no-win outcome)
/// </summary>
public class InstantWinEngine : IInstantWinEngine
{
    private readonly IEnumerable<IPrizeFilter> _filters;

    public InstantWinEngine(IEnumerable<IPrizeFilter> filters)
    {
        _filters = filters;
    }

    public async Task<Prize?> TrySelectPrizeAsync(
        IReadOnlyList<Prize> prizes,
        PrizeSelectionContext context,
        CancellationToken ct = default)
    {
        // 1. Run all filters against each prize to build eligible list
        var eligible = new List<Prize>();
        foreach (var prize in prizes)
        {
            var passes = true;
            foreach (var filter in _filters)
            {
                if (!await filter.IsEligibleAsync(prize, context, ct))
                {
                    passes = false;
                    break;
                }
            }
            if (passes)
                eligible.Add(prize);
        }

        // Determine whether any prize allows a no-win outcome
        bool allowNoWin = prizes.Any(p => p.AllowNoWin);

        // 2. No eligible prizes
        if (eligible.Count == 0)
        {
            // If AllowNoWin was set, a null (no-win) was the intentional outcome
            // Otherwise there's genuinely no stock — return null anyway
            return null;
        }

        // 3. Build random pool
        // If AllowNoWin=true: add a null slot giving the pool an equal chance of no-win
        if (allowNoWin)
        {
            // Pool: all eligible prizes + 1 null slot
            // Shuffle via GUID (equivalent to ORDER BY NEWID())
            var pool = new List<Prize?>(eligible) { null };
            return pool.OrderBy(_ => System.Guid.NewGuid()).First();
        }

        // 4. No null slot — always return a prize from the eligible pool
        return eligible.OrderBy(_ => System.Guid.NewGuid()).First();
    }
}
