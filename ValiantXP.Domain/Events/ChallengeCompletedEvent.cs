using MediatR;
using System;

namespace ValiantXP.Domain.Events;

public class ChallengeCompletedEvent : INotification
{
    public Guid UserId { get; }
    public Guid DynamicChallengeId { get; }
    public Guid UserChallengeProgressId { get; }

    public ChallengeCompletedEvent(Guid userId, Guid dynamicChallengeId, Guid userChallengeProgressId)
    {
        UserId = userId;
        DynamicChallengeId = dynamicChallengeId;
        UserChallengeProgressId = userChallengeProgressId;
    }
}
