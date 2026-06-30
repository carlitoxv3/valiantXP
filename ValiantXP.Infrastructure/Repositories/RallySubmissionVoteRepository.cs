using Microsoft.EntityFrameworkCore;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IRallySubmissionVoteRepository.
/// Mirrors PromoHub's ProfileRallyMultimediaVote operations in RallyRepository.
/// </summary>
public sealed class RallySubmissionVoteRepository : IRallySubmissionVoteRepository
{
    private readonly ApplicationDbContext _db;

    public RallySubmissionVoteRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RallySubmissionVote vote, CancellationToken ct = default)
    {
        await _db.RallySubmissionVotes.AddAsync(vote, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RallySubmissionVote?> GetByUserAndSubmissionAsync(
        Guid userId, Guid submissionId, CancellationToken ct = default)
    {
        return await _db.RallySubmissionVotes
            .FirstOrDefaultAsync(v => v.UserId == userId
                                   && v.RallySubmissionId == submissionId, ct);
    }

    public async Task<int> GetVoteCountAsync(Guid submissionId, CancellationToken ct = default)
    {
        return await _db.RallySubmissionVotes
            .CountAsync(v => v.RallySubmissionId == submissionId, ct);
    }

    public async Task<int> GetDailyVoteCountByUserAsync(
        Guid userId, Guid challengeId, DateTime date, CancellationToken ct = default)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return await _db.RallySubmissionVotes
            .Where(v => v.UserId == userId
                     && v.VotedAt >= dayStart
                     && v.VotedAt < dayEnd
                     && v.RallySubmission.DynamicChallengeId == challengeId)
            .CountAsync(ct);
    }
}
