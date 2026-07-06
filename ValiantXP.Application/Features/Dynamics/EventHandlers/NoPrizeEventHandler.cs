using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Events;

namespace ValiantXP.Application.Features.Dynamics.EventHandlers;

/// <summary>
/// Handles NoPrizeEvent — stub implementation for initial release.
/// Future: log to analytics table, track no-win rate, send consolation notification.
/// </summary>
public class NoPrizeEventHandler : INotificationHandler<NoPrizeEvent>
{
    private readonly ILogger<NoPrizeEventHandler> _logger;

    public NoPrizeEventHandler(ILogger<NoPrizeEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(NoPrizeEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "NoPrize: User={UserId} Challenge={ChallengeId} Reason={Reason}",
            notification.UserId,
            notification.ChallengeId,
            notification.Reason);

        // TODO: Analytics tracking — no-win rate per challenge
        // TODO: Optional consolation notification

        return Task.CompletedTask;
    }
}
