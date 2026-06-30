using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.Queries;

/// <summary>
/// Returns all submissions (any status) made by the authenticated user for a Rally challenge.
/// Mirrors PromoHub's GET /rally/getforuser endpoint.
/// Users can see their own pending/approved/rejected submissions to track status.
/// </summary>
public sealed record GetMyRallySubmissionsQuery(
    Guid ChallengeId,
    Guid UserId
) : IRequest<Result<IList<RallySubmissionDto>>>;

public sealed class GetMyRallySubmissionsQueryHandler
    : IRequestHandler<GetMyRallySubmissionsQuery, Result<IList<RallySubmissionDto>>>
{
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IRallySubmissionVoteRepository _voteRepo;

    public GetMyRallySubmissionsQueryHandler(
        IRallySubmissionRepository submissionRepo,
        IRallySubmissionVoteRepository voteRepo)
    {
        _submissionRepo = submissionRepo;
        _voteRepo = voteRepo;
    }

    public async Task<Result<IList<RallySubmissionDto>>> Handle(
        GetMyRallySubmissionsQuery request, CancellationToken ct)
    {
        var submissions = await _submissionRepo.GetByUserAsync(request.UserId, request.ChallengeId, ct);

        var dtos = new List<RallySubmissionDto>();
        foreach (var s in submissions)
        {
            var voteCount = await _voteRepo.GetVoteCountAsync(s.Id, ct);
            dtos.Add(new RallySubmissionDto
            {
                Id              = s.Id,
                SubmissionCode  = s.SubmissionCode,
                Status          = s.Status.ToString(),
                RallyType       = s.RallyType.ToString(),
                IsWinner        = s.IsWinner,
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

