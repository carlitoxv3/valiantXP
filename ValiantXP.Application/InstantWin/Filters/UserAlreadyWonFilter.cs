using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Filters;

/// <summary>
/// Blocks Product-type prizes if the user has already won that specific prize.
/// Mirrors the "User doesn't have that Product in History (Status != 5)" check in dbo.InstantWin_Calculate.
/// Only applies to PrizeType.Product — other types allow repeat wins.
/// </summary>
public class UserAlreadyWonFilter : IPrizeFilter
{
    private readonly IUserPrizeRepository _userPrizeRepo;

    public UserAlreadyWonFilter(IUserPrizeRepository userPrizeRepo)
    {
        _userPrizeRepo = userPrizeRepo;
    }

    public async Task<bool> IsEligibleAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct = default)
    {
        // Only enforce uniqueness for Product prizes (physical/digital products)
        if (prize.PrizeType != PrizeType.Product)
            return true;

        var alreadyWon = await _userPrizeRepo.UserAlreadyWonAsync(context.UserId, prize.Id, ct);
        return !alreadyWon;
    }
}
