using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

/// <summary>Repository for community votes on Rally submissions.</summary>
public interface IRallySubmissionVoteRepository
{
    Task AddAsync(RallySubmissionVote vote, CancellationToken ct = default);

    /// <summary>Returns existing vote by user for a submission, or null if not yet voted.</summary>
    Task<RallySubmissionVote?> GetByUserAndSubmissionAsync(Guid userId, Guid submissionId, CancellationToken ct = default);

    /// <summary>Total vote count for a submission (for display in gallery).</summary>
    Task<int> GetVoteCountAsync(Guid submissionId, CancellationToken ct = default);

    /// <summary>
    /// Count of votes cast by a user today for any submission in a challenge.
    /// Used by anti-fraud voting limit rule.
    /// </summary>
    Task<int> GetDailyVoteCountByUserAsync(Guid userId, Guid challengeId, DateTime date, CancellationToken ct = default);
}
