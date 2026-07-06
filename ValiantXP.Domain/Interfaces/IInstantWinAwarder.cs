using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

/// <summary>
/// Executes the award for a selected prize — creates UserPrize, decrements stock atomically,
/// creates UserPointMovement, and issues GiftCards as appropriate.
/// Equivalent to dbo.InstantWin_Save stored procedure in PromoHub.
/// </summary>
public interface IInstantWinAwarder
{
    Task<UserPrize> AwardAsync(
        Prize prize,
        PrizeSelectionContext context,
        CancellationToken ct = default);
}
