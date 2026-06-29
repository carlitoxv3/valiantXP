using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Commands;

public record SubmitChallengeCommand(Guid ChallengeId, Guid UserId, Dictionary<string, string> Inputs) : IRequest<Result<ChallengeResultDto>>;

public class SubmitChallengeCommandHandler : IRequestHandler<SubmitChallengeCommand, Result<ChallengeResultDto>>
{
    private readonly IDynamicService _dynamicService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public SubmitChallengeCommandHandler(IDynamicService dynamicService, IUnitOfWork unitOfWork, IPublisher publisher)
    {
        _dynamicService = dynamicService;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result<ChallengeResultDto>> Handle(SubmitChallengeCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify challenge exists and is active
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(request.ChallengeId, cancellationToken);
        if (challenge == null)
        {
            return Result<ChallengeResultDto>.Failure("Challenge not found.");
        }

        if (!challenge.IsActive)
        {
            return Result<ChallengeResultDto>.Failure("Challenge is inactive.");
        }

        // 2. Verify campaign is active
        var campaign = await _unitOfWork.Campaigns.GetAsync(challenge.CampaignId, cancellationToken);
        if (campaign == null)
        {
            return Result<ChallengeResultDto>.Failure("Campaign not found.");
        }

        if (!campaign.IsActive)
        {
            return Result<ChallengeResultDto>.Failure("Campaign is inactive.");
        }

        var now = DateTime.UtcNow;
        if (now < campaign.StartDate || now > campaign.EndDate)
        {
            return Result<ChallengeResultDto>.Failure("Campaign is not currently active.");
        }

        // 3. Check existing progress
        var progress = await _unitOfWork.UserChallengeProgresses.GetByUserAndChallengeAsync(request.UserId, request.ChallengeId, cancellationToken);
        if (progress != null && progress.Status == ChallengeStatus.Completed)
        {
            return Result<ChallengeResultDto>.Failure("You have already completed this challenge.");
        }

        // 4. Process dynamic execution via service/strategy
        var dynamicResult = await _dynamicService.ProcessDynamicAsync(request.ChallengeId, request.UserId, request.Inputs, cancellationToken);

        // Calculate score from payload if available
        int score = 0;
        if (dynamicResult.Payload is int intScore)
        {
            score = intScore;
        }
        else if (dynamicResult.Payload is IDictionary<string, object> dict && dict.TryGetValue("Score", out var scoreVal) && scoreVal is int dictScore)
        {
            score = dictScore;
        }

        // 5. Update or create user progress
        if (progress == null)
        {
            progress = new UserChallengeProgress
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                DynamicChallengeId = request.ChallengeId,
                Attempts = 1,
                Score = score,
                Status = dynamicResult.Success ? ChallengeStatus.Completed : ChallengeStatus.Failed,
                CompletedAt = dynamicResult.Success ? now : null
            };
            await _unitOfWork.UserChallengeProgresses.AddAsync(progress, cancellationToken);
        }
        else
        {
            progress.Attempts++;
            progress.Score = score;
            progress.Status = dynamicResult.Success ? ChallengeStatus.Completed : ChallengeStatus.Failed;
            progress.CompletedAt = dynamicResult.Success ? now : null;
            await _unitOfWork.UserChallengeProgresses.UpdateAsync(progress, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. If completed, publish Domain Event ChallengeCompletedEvent
        if (dynamicResult.Success)
        {
            var completedEvent = new ChallengeCompletedEvent(request.UserId, request.ChallengeId, progress.Id);
            await _publisher.Publish(completedEvent, cancellationToken);
        }

        // 7. Get awarded prizes to return in DTO
        var awardedPrizeNames = new List<string>();
        if (dynamicResult.Success)
        {
            var userPrizes = await _unitOfWork.UserPrizes.GetByUserIdAsync(request.UserId, cancellationToken);
            foreach (var up in userPrizes)
            {
                // Verify the prize belongs to this challenge and was awarded in this transaction (approx)
                if (up.Prize.DynamicChallengeId == request.ChallengeId && up.AwardedAt >= now.AddSeconds(-10))
                {
                    awardedPrizeNames.Add($"{up.Prize.Name} (Code: {up.Code})");
                }
            }
        }

        var resultDto = new ChallengeResultDto
        {
            Success = dynamicResult.Success,
            Message = dynamicResult.Message,
            Payload = dynamicResult.Payload,
            AwardedPrizeNames = awardedPrizeNames
        };

        return Result<ChallengeResultDto>.Success(resultDto);
    }
}
