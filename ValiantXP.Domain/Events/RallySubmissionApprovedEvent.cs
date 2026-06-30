using MediatR;

namespace ValiantXP.Domain.Events;

/// <summary>
/// Published when a Rally submission is approved by a moderator.
/// Handlers may: notify the submitting user, update gallery caches, trigger badge awards.
/// </summary>
public sealed record RallySubmissionApprovedEvent(
    Guid SubmissionId,
    Guid DynamicChallengeId,
    Guid UserId,
    string SubmissionCode
) : INotification;
