using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IRallySubmissionRepository.
/// Mirrors PromoHub's RallyRepository but adapted to Clean Architecture with GUIDs.
/// </summary>
public sealed class RallySubmissionRepository : IRallySubmissionRepository
{
    private readonly ApplicationDbContext _db;

    public RallySubmissionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RallySubmission submission, CancellationToken ct = default)
    {
        await _db.RallySubmissions.AddAsync(submission, ct);
    }

    public async Task<bool> UpdateAsync(RallySubmission submission, CancellationToken ct = default)
    {
        _db.RallySubmissions.Update(submission);
        return await _db.SaveChangesAsync(ct) > 0;
    }

    public async Task<RallySubmission?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.RallySubmissions
            .Include(s => s.User)
            .Include(s => s.Votes)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<RallySubmission?> GetByCodeAsync(string submissionCode, CancellationToken ct = default)
    {
        return await _db.RallySubmissions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SubmissionCode == submissionCode, ct);
    }

    public async Task<IList<RallySubmission>> GetApprovedAsync(Guid challengeId, CancellationToken ct = default)
    {
        return await _db.RallySubmissions
            .Where(s => s.DynamicChallengeId == challengeId
                     && s.Status == RallySubmissionStatus.Approved)
            .Include(s => s.User)
            .Include(s => s.Votes)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(ct);
    }

    public async Task<IList<RallySubmission>> GetPendingModerationAsync(Guid challengeId, CancellationToken ct = default)
    {
        return await _db.RallySubmissions
            .Where(s => s.DynamicChallengeId == challengeId
                     && s.Status == RallySubmissionStatus.PendingModeration)
            .Include(s => s.User)
            .OrderBy(s => s.SubmittedAt) // FIFO moderation queue
            .ToListAsync(ct);
    }

    public async Task<int> GetSubmissionCountAsync(
        Guid userId, Guid challengeId, DateTime windowStart, CancellationToken ct = default)
    {
        return await _db.RallySubmissions
            .CountAsync(s => s.UserId == userId
                          && s.DynamicChallengeId == challengeId
                          && s.Status != RallySubmissionStatus.Banned
                          && s.SubmittedAt >= windowStart, ct);
    }

    public async Task<bool> TicketExistsAsync(Guid challengeId, string nroTicket, CancellationToken ct = default)
    {
        // Check if ticketNumber appears in TicketDataJson using EF.Functions.Like or JSON value
        // Simple contains check — for production use JSON_VALUE query or indexed column
        return await _db.RallySubmissions
            .AnyAsync(s => s.DynamicChallengeId == challengeId
                        && s.TicketDataJson != null
                        && s.TicketDataJson.Contains(nroTicket), ct);
    }

    public async Task<IList<RallySubmission>> GetRankedByVotesAsync(Guid challengeId, CancellationToken ct = default)
    {
        // Load approved submissions with their vote counts, ordered by votes desc
        var submissions = await _db.RallySubmissions
            .Where(s => s.DynamicChallengeId == challengeId
                     && s.Status == RallySubmissionStatus.Approved)
            .Include(s => s.User)
            .Include(s => s.Votes)
            .ToListAsync(ct);

        return submissions
            .OrderByDescending(s => s.Votes.Count)
            .ThenByDescending(s => s.SubmittedAt)
            .ToList();
    }

    public async Task<IList<RallySubmission>> GetWinnersAsync(Guid challengeId, CancellationToken ct = default)
    {
        return await _db.RallySubmissions
            .Where(s => s.DynamicChallengeId == challengeId && s.IsWinner)
            .Include(s => s.User)
            .ToListAsync(ct);
    }

    /// <summary>
    /// All submissions by a specific user in a challenge (all statuses).
    /// Mirrors PromoHub's GetMultimediaByProfileIdRallyId.
    /// Used for: my-submissions view, sub-challenge availability check.
    /// </summary>
    public async Task<IList<RallySubmission>> GetByUserAsync(
        Guid userId, Guid challengeId, CancellationToken ct = default)
    {
        return await _db.RallySubmissions
            .Where(s => s.UserId == userId && s.DynamicChallengeId == challengeId)
            .Include(s => s.Votes)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(ct);
    }
}
