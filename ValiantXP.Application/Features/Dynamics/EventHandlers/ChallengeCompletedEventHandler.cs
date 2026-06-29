using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.EventHandlers;

public class ChallengeCompletedEventHandler : INotificationHandler<ChallengeCompletedEvent>
{
    private readonly IUnitOfWork _unitOfWork;

    public ChallengeCompletedEventHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ChallengeCompletedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Retrieve all prizes associated with this dynamic challenge
        var prizes = await _unitOfWork.Prizes.GetByChallengeIdAsync(notification.DynamicChallengeId, cancellationToken);

        // 2. Loop through and award prizes that have remaining quantity
        foreach (var prize in prizes)
        {
            if (prize.RemainingQuantity > 0)
            {
                // Decrement quantity
                prize.RemainingQuantity--;
                await _unitOfWork.Prizes.UpdateAsync(prize, cancellationToken);

                // Generate a unique code for the prize (e.g. VXP-XXXX-XXXX)
                var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
                var code = $"VXP-{prize.Type.ToUpperInvariant()}-{randomSuffix}";

                // Create UserPrize record
                var userPrize = new UserPrize
                {
                    Id = Guid.NewGuid(),
                    UserId = notification.UserId,
                    PrizeId = prize.Id,
                    AwardedAt = DateTime.UtcNow,
                    Code = code
                };

                await _unitOfWork.UserPrizes.AddAsync(userPrize, cancellationToken);
            }
        }

        // 3. Save all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
