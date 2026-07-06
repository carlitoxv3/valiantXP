using MediatR;
using System;

namespace ValiantXP.Domain.Events;

public class ChallengeCompletedEvent : INotification
{
    public Guid UserId { get; }
    public Guid DynamicChallengeId { get; }
    public Guid UserChallengeProgressId { get; }

    /// <summary>Optional Rally submission that triggered this event.</summary>
    public Guid? SubmissionId { get; }

    public ChallengeCompletedEvent(
        Guid userId,
        Guid dynamicChallengeId,
        Guid userChallengeProgressId,
        Guid? submissionId = null)
    {
        UserId = userId;
        DynamicChallengeId = dynamicChallengeId;
        UserChallengeProgressId = userChallengeProgressId;
        SubmissionId = submissionId;
    }
}
