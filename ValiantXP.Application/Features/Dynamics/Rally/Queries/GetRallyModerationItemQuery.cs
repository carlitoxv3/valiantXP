using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.Queries;

/// <summary>
/// Returns the detailed moderation record for a specific pending Rally submission.
/// Includes ticket data and any available audit/OCR log information.
/// Mirrors PromoHub's Admin GET /multimedia/moderation/{id} endpoint.
/// Admin-only.
/// </summary>
public sealed record GetRallyModerationItemQuery(Guid SubmissionId)
    : IRequest<Result<RallyModerationItemDto>>;

public sealed class GetRallyModerationItemQueryHandler
    : IRequestHandler<GetRallyModerationItemQuery, Result<RallyModerationItemDto>>
{
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IRallySubmissionVoteRepository _voteRepo;

    public GetRallyModerationItemQueryHandler(
        IRallySubmissionRepository submissionRepo,
        IRallySubmissionVoteRepository voteRepo)
    {
        _submissionRepo = submissionRepo;
        _voteRepo = voteRepo;
    }

    public async Task<Result<RallyModerationItemDto>> Handle(
        GetRallyModerationItemQuery request, CancellationToken ct)
    {
        var submission = await _submissionRepo.GetByIdAsync(request.SubmissionId, ct);
        if (submission is null)
            return Result<RallyModerationItemDto>.Failure("Submission not found.");

        var voteCount = await _voteRepo.GetVoteCountAsync(submission.Id, ct);

        return Result<RallyModerationItemDto>.Success(new RallyModerationItemDto
        {
            Id              = submission.Id,
            SubmissionCode  = submission.SubmissionCode,
            Status          = submission.Status.ToString(),
            RallyType       = submission.RallyType.ToString(),
            UserId          = submission.UserId,
            MediaUrl        = submission.MediaUrl,
            TextContent     = submission.TextContent,
            TicketDataJson  = submission.TicketDataJson,
            SubChallengeTag = submission.SubChallengeTag,
            VoteCount       = voteCount,
            SubmittedAt     = submission.SubmittedAt,
            ModeratedAt     = submission.ModeratedAt,
            ModeratedByUserId = submission.ModeratedByUserId,
            ModerationNotes = submission.ModerationNotes,
            RemoteIp        = submission.RemoteIp
        });
    }
}

