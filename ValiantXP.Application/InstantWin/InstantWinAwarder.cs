using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.InstantWin.Strategies;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin;

/// <summary>
/// Routes prize award to the correct IPrizeAwardStrategy based on prize type.
/// Implements IInstantWinAwarder — the orchestrator for the award phase.
/// </summary>
public class InstantWinAwarder : IInstantWinAwarder
{
    private readonly IEnumerable<IPrizeAwardStrategy> _strategies;

    public InstantWinAwarder(IEnumerable<IPrizeAwardStrategy> strategies)
    {
        _strategies = strategies;
    }

    public async Task<UserPrize> AwardAsync(
        Prize prize,
        PrizeSelectionContext context,
        CancellationToken ct = default)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(prize.PrizeType))
            ?? throw new NotSupportedException(
                $"No award strategy registered for PrizeType '{prize.PrizeType}'. " +
                $"Register an IPrizeAwardStrategy that handles this type.");

        return await strategy.AwardAsync(prize, context, ct);
    }
}
