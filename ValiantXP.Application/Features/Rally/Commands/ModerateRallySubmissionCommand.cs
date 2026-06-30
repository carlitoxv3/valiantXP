using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Rally.Commands;

/// <summary>
/// Moderates a Rally submission (admin action).
/// Transitions submission from PendingModeration to Approved, Rejected, or Banned.
/// When approved, publishes RallySubmissionApprovedEvent so handlers can notify the user.
/// </summary>
public sealed record ModerateRallySubmissionCommand(
    Guid SubmissionId,
    Guid ModeratorUserId,
    string Decision,    // "Approved" | "Rejected" | "Banned"
    string? Notes = null
) : IRequest<Result<ModerationResultDto>>;

public sealed class ModerateRallySubmissionCommandHandler
    : IRequestHandler<ModerateRallySubmissionCommand, Result<ModerationResultDto>>
{
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public ModerateRallySubmissionCommandHandler(
        IRallySubmissionRepository submissionRepo,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _submissionRepo = submissionRepo;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result<ModerationResultDto>> Handle(
        ModerateRallySubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var submission = await _submissionRepo.GetByIdAsync(request.SubmissionId, cancellationToken);
        if (submission is null)
            return Result<ModerationResultDto>.Failure("Submission not found.");

        if (submission.Status != RallySubmissionStatus.PendingModeration)
            return Result<ModerationResultDto>.Failure(
                $"Submission is already in '{submission.Status}' status and cannot be re-moderated.");

        // Parse decision
        if (!Enum.TryParse<RallySubmissionStatus>(request.Decision, ignoreCase: true, out var newStatus)
            || newStatus == RallySubmissionStatus.PendingModeration)
        {
            return Result<ModerationResultDto>.Failure(
                "Invalid decision. Use: 'Approved', 'Rejected', or 'Banned'.");
        }

        // Apply moderation
        submission.Status = newStatus;
        submission.ModeratedAt = DateTime.UtcNow;
        submission.ModeratedByUserId = request.ModeratorUserId;
        submission.ModerationNotes = request.Notes;

        await _submissionRepo.UpdateAsync(submission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event when approved — triggers user notification handlers
        if (newStatus == RallySubmissionStatus.Approved)
        {
            await _publisher.Publish(new RallySubmissionApprovedEvent(
                submission.Id,
                submission.DynamicChallengeId,
                submission.UserId,
                submission.SubmissionCode),
                cancellationToken);
        }

        return Result<ModerationResultDto>.Success(new ModerationResultDto
        {
            Success = true,
            Message = $"Submission {submission.SubmissionCode} has been {newStatus.ToString().ToLower()}.",
            NewStatus = newStatus.ToString()
        });
    }
}
