using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.Queries;

/// <summary>
/// Returns submissions marked as winners for a Rally challenge (public endpoint).
/// Mirrors PromoHub's GET /rally/winners endpoint.
/// </summary>
public sealed record GetRallyWinnersQuery(Guid ChallengeId) : IRequest<Result<IList<RallySubmissionDto>>>;

public sealed class GetRallyWinnersQueryHandler
    : IRequestHandler<GetRallyWinnersQuery, Result<IList<RallySubmissionDto>>>
{
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IRallySubmissionVoteRepository _voteRepo;

    public GetRallyWinnersQueryHandler(
        IRallySubmissionRepository submissionRepo,
        IRallySubmissionVoteRepository voteRepo)
    {
        _submissionRepo = submissionRepo;
        _voteRepo = voteRepo;
    }

    public async Task<Result<IList<RallySubmissionDto>>> Handle(
        GetRallyWinnersQuery request, CancellationToken ct)
    {
        var winners = await _submissionRepo.GetWinnersAsync(request.ChallengeId, ct);

        var dtos = new List<RallySubmissionDto>();
        foreach (var s in winners)
        {
            var voteCount = await _voteRepo.GetVoteCountAsync(s.Id, ct);
            dtos.Add(MapToDto(s, voteCount));
        }

        return Result<IList<RallySubmissionDto>>.Success(dtos);
    }

    private static RallySubmissionDto MapToDto(Domain.Entities.RallySubmission s, int voteCount) =>
        new()
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
        };
}

