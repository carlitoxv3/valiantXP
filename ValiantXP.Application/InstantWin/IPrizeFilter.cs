using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin;

/// <summary>
/// Eligibility filter in the InstantWin prize selection pipeline.
/// Each filter implements one business rule from dbo.InstantWin_Calculate.
/// </summary>
public interface IPrizeFilter
{
    Task<bool> IsEligibleAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct = default);
}
