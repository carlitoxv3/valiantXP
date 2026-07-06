using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.EventHandlers;

/// <summary>
/// Handles ChallengeCompletedEvent by running the InstantWin engine:
///   1. Load prizes for the challenge
///   2. Build PrizeSelectionContext from event data
///   3. TrySelectPrize via IInstantWinEngine (filters + random selection)
///   4. If prize selected: AwardAsync via IInstantWinAwarder (strategy-based award)
///   5. Publish PrizeAwardedEvent or NoPrizeEvent
///   6. Always log attempt (mirrors PromoHub's Azure Table LogInstantWin)
///
/// Replaces the old handler that awarded ALL prizes without selection/stock control.
/// </summary>
public class ChallengeCompletedEventHandler : INotificationHandler<ChallengeCompletedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInstantWinEngine _engine;
    private readonly IInstantWinAwarder _awarder;
    private readonly IPublisher _publisher;
    private readonly ILogger<ChallengeCompletedEventHandler> _logger;

    public ChallengeCompletedEventHandler(
        IUnitOfWork unitOfWork,
        IInstantWinEngine engine,
        IInstantWinAwarder awarder,
        IPublisher publisher,
        ILogger<ChallengeCompletedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _engine = engine;
        _awarder = awarder;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(ChallengeCompletedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Load prizes for this challenge
        var prizes = (await _unitOfWork.Prizes.GetByChallengeIdAsync(
            notification.DynamicChallengeId, cancellationToken)).ToList();

        if (prizes.Count == 0)
        {
            _logger.LogDebug(
                "InstantWin: No prizes configured for challenge {ChallengeId}",
                notification.DynamicChallengeId);
            return;
        }

        // 2. Build selection context
        var context = new PrizeSelectionContext
        {
            UserId = notification.UserId,
            ChallengeId = notification.DynamicChallengeId,
            SubmissionId = notification.SubmissionId,
            Now = DateTime.UtcNow
        };

        // 3. Select prize (runs eligibility filter chain + random selection)
        var selectedPrize = await _engine.TrySelectPrizeAsync(prizes, context, cancellationToken);

        // Always log attempt — mirrors PromoHub's Azure Table "LogInstantWin"
        _logger.LogInformation(
            "InstantWin attempt: User={UserId} Challenge={ChallengeId} SelectedPrize={PrizeId}",
            context.UserId,
            context.ChallengeId,
            selectedPrize?.Id.ToString() ?? "null (no-win)");

        // 4. No prize selected (AllowNoWin or all stock depleted)
        if (selectedPrize is null)
        {
            await _publisher.Publish(
                new NoPrizeEvent(
                    context.UserId,
                    context.ChallengeId,
                    "No eligible prizes or null slot selected"),
                cancellationToken);
            return;
        }

        // 5. Award the prize via strategy
        var userPrize = await _awarder.AwardAsync(selectedPrize, context, cancellationToken);

        // 6. Publish success event
        await _publisher.Publish(
            new PrizeAwardedEvent(
                userPrize.UserId,
                userPrize.PrizeId,
                userPrize.Id,
                userPrize.PrizeType,
                userPrize.PointsAwarded),
            cancellationToken);
    }
}
