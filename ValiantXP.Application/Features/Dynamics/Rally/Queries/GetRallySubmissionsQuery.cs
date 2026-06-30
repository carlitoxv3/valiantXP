using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.Queries;

/// <summary>
/// Returns the approved submissions gallery for a Rally challenge.
/// Submissions are ordered by vote count descending, then by submission date.
/// </summary>
public sealed record GetRallySubmissionsQuery(
    Guid ChallengeId,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<IList<RallySubmissionDto>>>;

public sealed class GetRallySubmissionsQueryHandler
    : IRequestHandler<GetRallySubmissionsQuery, Result<IList<RallySubmissionDto>>>
{
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IRallySubmissionVoteRepository _voteRepo;

    public GetRallySubmissionsQueryHandler(
        IRallySubmissionRepository submissionRepo,
        IRallySubmissionVoteRepository voteRepo)
    {
        _submissionRepo = submissionRepo;
        _voteRepo = voteRepo;
    }

    public async Task<Result<IList<RallySubmissionDto>>> Handle(
        GetRallySubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var submissions = await _submissionRepo.GetRankedByVotesAsync(request.ChallengeId, cancellationToken);

        // Paginate in-memory (repository already loads only approved ones)
        var paged = submissions
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = new List<RallySubmissionDto>();
        foreach (var s in paged)
        {
            var voteCount = await _voteRepo.GetVoteCountAsync(s.Id, cancellationToken);
            dtos.Add(new RallySubmissionDto
            {
                Id = s.Id,
                SubmissionCode = s.SubmissionCode,
                Status = s.Status.ToString(),
                RallyType = s.RallyType.ToString(),
                IsWinner = s.IsWinner,
                MediaUrl = s.MediaUrl,
                TextContent = s.TextContent,
                VoteCount = voteCount,
                SubChallengeTag = s.SubChallengeTag,
                SubmittedAt = s.SubmittedAt
            });
        }

        return Result<IList<RallySubmissionDto>>.Success(dtos);
    }
}

