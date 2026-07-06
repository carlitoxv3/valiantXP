using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Filters;

/// <summary>
/// Enforces the MaxPerUserInWindow limit: at most N awards of this prize per user within WindowHours.
/// Maps to "COUNT(ProfileMovements.CouponDetailId) &lt; QuantityToUsePerUser" in dbo.InstantWin_Calculate.
/// </summary>
public class PerUserWindowFilter : IPrizeFilter
{
    private readonly IUserPrizeRepository _userPrizeRepo;

    public PerUserWindowFilter(IUserPrizeRepository userPrizeRepo)
    {
        _userPrizeRepo = userPrizeRepo;
    }

    public async Task<bool> IsEligibleAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct = default)
    {
        // 0 = unlimited per user
        if (prize.MaxPerUserInWindow == 0)
            return true;

        var windowStart = context.Now.AddHours(-prize.WindowHours);
        var count = await _userPrizeRepo.GetUserAwardCountInWindowAsync(
            context.UserId, prize.Id, windowStart, ct);

        return count < prize.MaxPerUserInWindow;
    }
}
