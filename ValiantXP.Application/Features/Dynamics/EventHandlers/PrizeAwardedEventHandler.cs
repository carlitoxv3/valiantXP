using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Events;

namespace ValiantXP.Application.Features.Dynamics.EventHandlers;

/// <summary>
/// Handles PrizeAwardedEvent — stub implementation for initial release.
/// Future: send push notification, refresh rank cache, trigger CRM webhook.
/// Mirrors PromoHub's post-save trigger logic (Trivia, Memorama notifications, etc.)
/// </summary>
public class PrizeAwardedEventHandler : INotificationHandler<PrizeAwardedEvent>
{
    private readonly ILogger<PrizeAwardedEventHandler> _logger;

    public PrizeAwardedEventHandler(ILogger<PrizeAwardedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PrizeAwardedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PrizeAwarded: User={UserId} Prize={PrizeId} UserPrize={UserPrizeId} Type={PrizeType} Points={Points}",
            notification.UserId,
            notification.PrizeId,
            notification.UserPrizeId,
            notification.PrizeType,
            notification.PointsAwarded);

        // TODO: Send push/email notification
        // TODO: Refresh rank cache
        // TODO: CRM webhook

        return Task.CompletedTask;
    }
}
