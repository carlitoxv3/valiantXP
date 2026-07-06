using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Filters;

/// <summary>
/// Enforces the MaxGlobalInWindow limit: at most N awards of this prize globally within WindowHours.
/// Maps to the "COUNT(History) &lt; QuantityToUse within DelayInHours" check in dbo.InstantWin_Calculate.
/// </summary>
public class GlobalWindowFilter : IPrizeFilter
{
    private readonly IUserPrizeRepository _userPrizeRepo;

    public GlobalWindowFilter(IUserPrizeRepository userPrizeRepo)
    {
        _userPrizeRepo = userPrizeRepo;
    }

    public async Task<bool> IsEligibleAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct = default)
    {
        // 0 = unlimited — always eligible
        if (prize.MaxGlobalInWindow == 0)
            return true;

        // No time window configured — no global limit applies
        if (prize.WindowHours == 0)
            return true;

        var windowStart = context.Now.AddHours(-prize.WindowHours);
        var count = await _userPrizeRepo.GetAwardCountInWindowAsync(prize.Id, windowStart, ct);

        return count < prize.MaxGlobalInWindow;
    }
}
