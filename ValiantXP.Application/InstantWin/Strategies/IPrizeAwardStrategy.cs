using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Strategies;

/// <summary>
/// Award strategy for a specific prize type.
/// Each strategy implements the award logic for one PrizeType
/// (analogous to the branching in dbo.InstantWin_Save).
/// </summary>
public interface IPrizeAwardStrategy
{
    bool CanHandle(PrizeType prizeType);

    Task<UserPrize> AwardAsync(
        Prize prize,
        PrizeSelectionContext context,
        CancellationToken ct);
}
