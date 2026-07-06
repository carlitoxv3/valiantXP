using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

/// <summary>
/// Selects a prize from the available pool applying eligibility filters and random selection.
/// Equivalent to dbo.InstantWin_Calculate stored procedure in PromoHub.
/// Returns null when AllowNoWin is true and the null slot is selected (no-win outcome).
/// </summary>
public interface IInstantWinEngine
{
    Task<Prize?> TrySelectPrizeAsync(
        IReadOnlyList<Prize> prizes,
        PrizeSelectionContext context,
        CancellationToken ct = default);
}
