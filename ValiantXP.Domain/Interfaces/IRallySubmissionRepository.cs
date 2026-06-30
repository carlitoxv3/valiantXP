using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Interfaces;

/// <summary>
/// Repository for Rally submissions (UGC entries).
/// Provides CRUD and query operations for moderation, gallery, and winner selection flows.
/// </summary>
public interface IRallySubmissionRepository
{
    // ─── Write ────────────────────────────────────────────────────────────────

    Task AddAsync(RallySubmission submission, CancellationToken ct = default);
    Task<bool> UpdateAsync(RallySubmission submission, CancellationToken ct = default);

    // ─── Read ─────────────────────────────────────────────────────────────────

    Task<RallySubmission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RallySubmission?> GetByCodeAsync(string submissionCode, CancellationToken ct = default);

    /// <summary>All submissions for a challenge (gallery — only Approved).</summary>
    Task<IList<RallySubmission>> GetApprovedAsync(Guid challengeId, CancellationToken ct = default);

    /// <summary>All submissions pending moderator review.</summary>
    Task<IList<RallySubmission>> GetPendingModerationAsync(Guid challengeId, CancellationToken ct = default);

    /// <summary>
    /// Count of submissions by a user within a time window.
    /// Used by RallySubmissionLimitRule.
    /// </summary>
    Task<int> GetSubmissionCountAsync(Guid userId, Guid challengeId, DateTime windowStart, CancellationToken ct = default);

    /// <summary>
    /// Check if a ticket number already exists in a rally challenge (uniqueness validation).
    /// Used by RallyTicketUniquenessRule.
    /// </summary>
    Task<bool> TicketExistsAsync(Guid challengeId, string nroTicket, CancellationToken ct = default);

    /// <summary>All submissions for a challenge ordered by vote count descending (for ByVotes winner selection).</summary>
    Task<IList<RallySubmission>> GetRankedByVotesAsync(Guid challengeId, CancellationToken ct = default);

    /// <summary>Returns all winner submissions for a challenge.</summary>
    Task<IList<RallySubmission>> GetWinnersAsync(Guid challengeId, CancellationToken ct = default);

    /// <summary>
    /// All submissions by a specific user for a challenge (all statuses).
    /// Used for the 'My Submissions' view and sub-challenge availability check.
    /// Mirrors PromoHub's GetMultimediaByProfileIdRallyId.
    /// </summary>
    Task<IList<RallySubmission>> GetByUserAsync(Guid userId, Guid challengeId, CancellationToken ct = default);
}
