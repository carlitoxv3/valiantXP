using MediatR;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.EventHandlers;

/// <summary>
/// Handles RallyWinnerSelectedEvent by triggering prize assignment for each winner
/// through the existing ChallengeCompletedEvent → ChallengeCompletedEventHandler pipeline.
///
/// This bridges the async Rally flow (winner selected later) with the sync dynamics flow
/// (ChallengeCompletedEvent fires prize assignment). Each winner gets their own event.
/// </summary>
public sealed class RallyWinnerSelectedEventHandler : INotificationHandler<RallyWinnerSelectedEvent>
{
    private readonly IPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRallySubmissionRepository _submissionRepo;

    public RallyWinnerSelectedEventHandler(
        IPublisher publisher,
        IUnitOfWork unitOfWork,
        IRallySubmissionRepository submissionRepo)
    {
        _publisher = publisher;
        _unitOfWork = unitOfWork;
        _submissionRepo = submissionRepo;
    }

    public async Task Handle(RallyWinnerSelectedEvent notification, CancellationToken cancellationToken)
    {
        // For each winner, create a UserChallengeProgress record (Completed)
        // and fire ChallengeCompletedEvent to trigger ChallengeCompletedEventHandler (prize assignment).
        foreach (var (submissionId, userId) in
            notification.WinnerSubmissionIds.Zip(notification.WinnerUserIds))
        {
            // Create or update progress record to mark challenge as completed for this winner
            var progress = await _unitOfWork.UserChallengeProgresses
                .GetByUserAndChallengeAsync(userId, notification.DynamicChallengeId, cancellationToken);

            if (progress is null)
            {
                progress = new UserChallengeProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DynamicChallengeId = notification.DynamicChallengeId,
                    Status = Domain.Enums.ChallengeStatus.Completed,
                    Attempts = 1,
                    Score = 100, // Winners always get max score
                    CompletedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserChallengeProgresses.AddAsync(progress, cancellationToken);
            }
            else
            {
                progress.Status = Domain.Enums.ChallengeStatus.Completed;
                progress.CompletedAt = DateTime.UtcNow;
                await _unitOfWork.UserChallengeProgresses.UpdateAsync(progress, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Fire ChallengeCompletedEvent per winner → ChallengeCompletedEventHandler assigns prizes
            await _publisher.Publish(
                new ChallengeCompletedEvent(userId, notification.DynamicChallengeId, progress.Id),
                cancellationToken);
        }
    }
}

