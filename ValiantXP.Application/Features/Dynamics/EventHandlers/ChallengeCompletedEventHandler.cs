using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
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
/// Position-Based InstantWin (Meseros flow):
///   If the user has a pending reservation (ReservedPrizeId) from a Code challenge
///   in the same campaign, the reserved prize is confirmed here instead of running
///   the normal random InstantWin engine.
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
    private readonly IUserChallengeProgressRepository _progressRepo;
    private readonly IUserPointMovementRepository _pointRepo;

    public ChallengeCompletedEventHandler(
        IUnitOfWork unitOfWork,
        IInstantWinEngine engine,
        IInstantWinAwarder awarder,
        IPublisher publisher,
        ILogger<ChallengeCompletedEventHandler> logger,
        IUserChallengeProgressRepository progressRepo,
        IUserPointMovementRepository pointRepo)
    {
        _unitOfWork = unitOfWork;
        _engine = engine;
        _awarder = awarder;
        _publisher = publisher;
        _logger = logger;
        _progressRepo = progressRepo;
        _pointRepo = pointRepo;
    }

    public async Task Handle(ChallengeCompletedEvent notification, CancellationToken cancellationToken)
    {
        // ─── Position-Based Prize Confirmation (Meseros flow) ────────────────────
        // If this Trivia completion follows a Code step that reserved a prize,
        // confirm the reserved prize instead of running random InstantWin.
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(
            notification.DynamicChallengeId, cancellationToken);

        if (challenge != null)
        {
            var codeProgress = await _progressRepo.GetLatestCodeProgressWithReservationAsync(
                notification.UserId, challenge.CampaignId, cancellationToken);

            if (codeProgress?.ReservedPrizeId != null)
            {
                var reservedPrize = await _unitOfWork.Prizes.GetAsync(
                    codeProgress.ReservedPrizeId.Value, cancellationToken);

                if (reservedPrize != null && reservedPrize.RemainingQuantity > 0)
                {
                    _logger.LogInformation(
                        "PositionWin: confirming reserved prize {PrizeId} for User={UserId} after Trivia {ChallengeId}",
                        reservedPrize.Id, notification.UserId, notification.DynamicChallengeId);

                    // Decrement stock atomically via EF (optimistic, row still has stock)
                    reservedPrize.RemainingQuantity--;
                    await _unitOfWork.Prizes.UpdateAsync(reservedPrize, cancellationToken);

                    // Create UserPrize
                    var userPrize = new UserPrize
                    {
                        Id = Guid.NewGuid(),
                        UserId = notification.UserId,
                        PrizeId = reservedPrize.Id,
                        PrizeType = reservedPrize.PrizeType,
                        PointsAwarded = reservedPrize.PrizeType == PrizeType.Points ? reservedPrize.Quantity : 0,
                        Code = $"VXP-{reservedPrize.PrizeType}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                        AwardedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.UserPrizes.AddAsync(userPrize, cancellationToken);

                    // Point movement ledger entry (for Points-type prizes)
                    if (reservedPrize.PrizeType == PrizeType.Points)
                    {
                        var movement = new UserPointMovement
                        {
                            Id = Guid.NewGuid(),
                            UserId = notification.UserId,
                            Points = reservedPrize.Quantity,
                            Source = "PositionWin",
                            Description = $"Premio: {reservedPrize.Name}",
                            ChallengeId = notification.DynamicChallengeId,
                            PrizeId = reservedPrize.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _pointRepo.AddAsync(movement, cancellationToken);
                    }

                    // Clear reservation
                    codeProgress.ReservedPrizeId = null;
                    await _unitOfWork.UserChallengeProgresses.UpdateAsync(codeProgress, cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Publish success event
                    await _publisher.Publish(new PrizeAwardedEvent(
                        notification.UserId,
                        reservedPrize.Id,
                        userPrize.Id,
                        reservedPrize.PrizeType,
                        userPrize.PointsAwarded),
                        cancellationToken);

                    return; // Do NOT run normal InstantWin for this completion
                }
            }
        }
        // ─────────────────────────────────────────────────────────────────────────

        // 1. Load prizes for this challenge (normal InstantWin path)
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
        var awarded = await _awarder.AwardAsync(selectedPrize, context, cancellationToken);

        // 6. Publish success event
        await _publisher.Publish(
            new PrizeAwardedEvent(
                awarded.UserId,
                awarded.PrizeId,
                awarded.Id,
                awarded.PrizeType,
                awarded.PointsAwarded),
            cancellationToken);
    }
}
