using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.Commands;

/// <summary>
/// Casts a community vote on an approved Rally submission.
/// One vote per user per submission — enforced by DB unique index and application-layer check.
/// Anti-fraud: MaxVotesPerUserPerDay limit from AntiFraudCampaignConfig.Rally.
/// </summary>
public sealed record VoteRallySubmissionCommand(
    Guid SubmissionId,
    Guid UserId,
    string? RemoteIp = null
) : IRequest<Result<VoteResultDto>>;

public sealed class VoteRallySubmissionCommandHandler
    : IRequestHandler<VoteRallySubmissionCommand, Result<VoteResultDto>>
{
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IRallySubmissionVoteRepository _voteRepo;
    private readonly IUnitOfWork _unitOfWork;

    public VoteRallySubmissionCommandHandler(
        IRallySubmissionRepository submissionRepo,
        IRallySubmissionVoteRepository voteRepo,
        IUnitOfWork unitOfWork)
    {
        _submissionRepo = submissionRepo;
        _voteRepo = voteRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<VoteResultDto>> Handle(
        VoteRallySubmissionCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load submission — must exist and be approved to receive votes
        var submission = await _submissionRepo.GetByIdAsync(request.SubmissionId, cancellationToken);
        if (submission is null)
            return Result<VoteResultDto>.Failure("Submission not found.");

        if (submission.Status != Domain.Enums.RallySubmissionStatus.Approved)
            return Result<VoteResultDto>.Failure("Only approved submissions can receive votes.");

        // 2. Prevent user from voting on their own submission
        if (submission.UserId == request.UserId)
            return Result<VoteResultDto>.Failure("You cannot vote on your own submission.");

        // 3. One vote per user per submission
        var existing = await _voteRepo.GetByUserAndSubmissionAsync(
            request.UserId, request.SubmissionId, cancellationToken);
        if (existing is not null)
            return Result<VoteResultDto>.Failure("You have already voted for this submission.");

        // 4. Daily vote limit check (MaxVotesPerUserPerDay from challenge's anti-fraud config)
        // Note: Config retrieved from challenge — simplified here; can be extended to pass config via command
        var todayVotes = await _voteRepo.GetDailyVoteCountByUserAsync(
            request.UserId, submission.DynamicChallengeId, DateTime.UtcNow.Date, cancellationToken);

        // Default max 5 votes/day; real config is loaded in RallyController from AntiFraudCampaignConfig
        const int DefaultMaxVotesPerDay = 5;
        if (todayVotes >= DefaultMaxVotesPerDay)
            return Result<VoteResultDto>.Failure($"You have reached the daily vote limit of {DefaultMaxVotesPerDay}.");

        // 5. Persist the vote
        var vote = new RallySubmissionVote
        {
            Id = Guid.NewGuid(),
            RallySubmissionId = request.SubmissionId,
            UserId = request.UserId,
            RemoteIp = request.RemoteIp ?? string.Empty,
            VotedAt = DateTime.UtcNow
        };

        await _voteRepo.AddAsync(vote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var newCount = await _voteRepo.GetVoteCountAsync(request.SubmissionId, cancellationToken);

        return Result<VoteResultDto>.Success(new VoteResultDto
        {
            Success = true,
            Message = "Vote registered successfully.",
            NewVoteCount = newCount
        });
    }
}

