using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.Queries;

/// <summary>
/// Returns all pending-moderation submissions for a Rally challenge, ordered FIFO (oldest first).
/// Dedicated query replacing the previous hack that reused GetRallySubmissionsQuery.
/// Admin-only. Mirrors PromoHub Admin's POST /multimedia/moderation/list.
/// </summary>
public sealed record GetRallyPendingQueueQuery(
    Guid ChallengeId,
    int Page = 1,
    int PageSize = 50
) : IRequest<Result<IList<RallySubmissionDto>>>;

public sealed class GetRallyPendingQueueQueryHandler
    : IRequestHandler<GetRallyPendingQueueQuery, Result<IList<RallySubmissionDto>>>
{
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IRallySubmissionVoteRepository _voteRepo;

    public GetRallyPendingQueueQueryHandler(
        IRallySubmissionRepository submissionRepo,
        IRallySubmissionVoteRepository voteRepo)
    {
        _submissionRepo = submissionRepo;
        _voteRepo = voteRepo;
    }

    public async Task<Result<IList<RallySubmissionDto>>> Handle(
        GetRallyPendingQueueQuery request, CancellationToken ct)
    {
        var pending = await _submissionRepo.GetPendingModerationAsync(request.ChallengeId, ct);

        var paged = pending
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = new List<RallySubmissionDto>();
        foreach (var s in paged)
        {
            var voteCount = await _voteRepo.GetVoteCountAsync(s.Id, ct);
            dtos.Add(new RallySubmissionDto
            {
                Id              = s.Id,
                SubmissionCode  = s.SubmissionCode,
                Status          = s.Status.ToString(),
                RallyType       = s.RallyType.ToString(),
                IsWinner        = false,
                MediaUrl        = s.MediaUrl,
                TextContent     = s.TextContent,
                VoteCount       = voteCount,
                SubChallengeTag = s.SubChallengeTag,
                SubmittedAt     = s.SubmittedAt
            });
        }

        return Result<IList<RallySubmissionDto>>.Success(dtos);
    }
}

