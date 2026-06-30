using MediatR;

namespace ValiantXP.Domain.Events;

/// <summary>
/// Published when one or more winners are selected for a Rally challenge.
/// This is the event that triggers prize assignment (equivalent to ChallengeCompletedEvent for other dynamics).
/// Handlers: ChallengeCompletedEventHandler will be invoked per winner to assign prizes.
/// </summary>
public sealed record RallyWinnerSelectedEvent(
    Guid DynamicChallengeId,
    Guid CampaignId,
    IReadOnlyList<Guid> WinnerSubmissionIds,
    IReadOnlyList<Guid> WinnerUserIds
) : INotification;
