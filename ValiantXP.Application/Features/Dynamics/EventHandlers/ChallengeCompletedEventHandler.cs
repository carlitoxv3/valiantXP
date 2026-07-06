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
/// Position-Based InstantWin (universal):
///   If the user has a pending reservation (ReservedPrizeId) from a previous challenge
///   in the same campaign (set by IPositionWinService), the reserved prize is confirmed
///   here (via ConfirmReservedPrizeAsync) BEFORE running the normal base InstantWin.
///   Both can coexist: positional prize gets confirmed AND base InstantWin runs.
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
        var ct = cancellationToken;

        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(
            notification.DynamicChallengeId, ct);

        // ─── Position-Based Prize Confirmation ──────────────────────────────────
        // Check if this challenge completion should confirm a positional prize
        // reservation set by IPositionWinService during a previous challenge submission.
        if (challenge != null)
        {
            var codeProgress = await _progressRepo.GetLatestCodeProgressWithReservationAsync(
                notification.UserId, challenge.CampaignId, ct);

            if (codeProgress?.ReservedPrizeId != null)
            {
                _logger.LogInformation(
                    "PositionWin confirmation: User={UserId} ReservedPrize={PrizeId}",
                    notification.UserId, codeProgress.ReservedPrizeId);

                await ConfirmReservedPrizeAsync(
                    notification.UserId,
                    codeProgress.ReservedPrizeId.Value,
                    notification.DynamicChallengeId,
                    ct);

                // Clear reservation
                codeProgress.ReservedPrizeId = null;
                await _unitOfWork.UserChallengeProgresses.UpdateAsync(codeProgress, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                // Still continue to award base rewards below (don't return early)
            }
        }
        // ────────────────────────────────────────────────────────────────────────

        // 1. Load prizes for this challenge (base InstantWin path only — exclude positional)
        var prizes = (await _unitOfWork.Prizes.GetByChallengeIdAsync(
            notification.DynamicChallengeId, ct))
            .Where(p => !p.IsPositionalReward)  // exclude positional prizes from normal InstantWin
            .ToList();

        if (prizes.Count == 0)
        {
            _logger.LogDebug(
                "InstantWin: No base prizes configured for challenge {ChallengeId}",
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
        var selectedPrize = await _engine.TrySelectPrizeAsync(prizes, context, ct);

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
                ct);
            return;
        }

        // 5. Award the prize via strategy
        var awarded = await _awarder.AwardAsync(selectedPrize, context, ct);

        // 6. Publish success event
        await _publisher.Publish(
            new PrizeAwardedEvent(
                awarded.UserId,
                awarded.PrizeId,
                awarded.Id,
                awarded.PrizeType,
                awarded.PointsAwarded),
            ct);
    }

    /// <summary>
    /// Confirms a positional prize that was reserved by IPositionWinService.
    /// Performs atomic stock decrement and creates the UserPrize + point ledger entry.
    /// </summary>
    private async Task ConfirmReservedPrizeAsync(
        Guid userId, Guid prizeId, Guid challengeId, CancellationToken ct)
    {
        var prize = await _unitOfWork.Prizes.GetAsync(prizeId, ct);
        if (prize == null)
        {
            _logger.LogWarning("PositionWin: Prize {PrizeId} not found on confirmation", prizeId);
            return;
        }

        // Optimistic stock decrement — check remaining quantity
        if (prize.RemainingQuantity <= 0)
        {
            _logger.LogWarning(
                "PositionWin: Prize {PrizeId} out of stock on confirmation for User={UserId}",
                prizeId, userId);
            return;
        }

        prize.RemainingQuantity--;
        await _unitOfWork.Prizes.UpdateAsync(prize, ct);

        var code = $"VXP-POS-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var userPrize = new UserPrize
        {
            Id            = Guid.NewGuid(),
            UserId        = userId,
            PrizeId       = prizeId,
            PrizeType     = prize.PrizeType,
            PointsAwarded = prize.PrizeType == PrizeType.Points ? prize.Quantity : 0,
            Code          = code,
            AwardedAt     = DateTime.UtcNow,
        };
        await _unitOfWork.UserPrizes.AddAsync(userPrize, ct);

        if (prize.PrizeType == PrizeType.Points)
        {
            var movement = new UserPointMovement
            {
                Id          = Guid.NewGuid(),
                UserId      = userId,
                Points      = prize.Quantity,
                Source      = "PositionWin",
                Description = $"Premio posicional: {prize.Name}",
                ChallengeId = challengeId,
                PrizeId     = prizeId,
                CreatedAt   = DateTime.UtcNow,
            };
            await _pointRepo.AddAsync(movement, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "PositionWin: confirmed prize {PrizeId} ({PrizeName}) for User={UserId}",
            prizeId, prize.Name, userId);

        await _publisher.Publish(new PrizeAwardedEvent(
            userId, prizeId, userPrize.Id, prize.PrizeType, userPrize.PointsAwarded), ct);
    }
}
