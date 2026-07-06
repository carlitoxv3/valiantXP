using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IUserChallengeProgressRepository : IRepository<UserChallengeProgress>
{
    Task<UserChallengeProgress?> GetByUserAndChallengeAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts completed submissions for a given challenge on a specific UTC day.
    /// Used to determine a user's daily position in position_based challenges.
    /// </summary>
    Task<int> GetDailyCompletionCountAsync(Guid challengeId, DateTime date, CancellationToken ct = default);

    /// <summary>
    /// Returns the most recent Code-challenge progress record for the user in the
    /// given campaign that has a non-null ReservedPrizeId. Includes DynamicChallenge.
    /// Used by ChallengeCompletedEventHandler to confirm a reserved prize.
    /// </summary>
    Task<UserChallengeProgress?> GetLatestCodeProgressWithReservationAsync(Guid userId, Guid campaignId, CancellationToken ct = default);
}
