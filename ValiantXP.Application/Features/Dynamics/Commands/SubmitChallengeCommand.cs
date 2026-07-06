using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.AntiFraud;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Commands;

public record SubmitChallengeCommand(Guid ChallengeId, Guid UserId, Dictionary<string, string> Inputs, string? RemoteIp = null)
    : IRequest<Result<ChallengeResultDto>>;

public class SubmitChallengeCommandHandler : IRequestHandler<SubmitChallengeCommand, Result<ChallengeResultDto>>
{
    private readonly IDynamicService _dynamicService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly IAntiFraudPipeline _antiFraudPipeline;
    private readonly IPositionWinService _positionWinService;

    public SubmitChallengeCommandHandler(
        IDynamicService dynamicService,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        IAntiFraudPipeline antiFraudPipeline,
        IPositionWinService positionWinService)
    {
        _dynamicService = dynamicService;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _antiFraudPipeline = antiFraudPipeline;
        _positionWinService = positionWinService;
    }

    public async Task<Result<ChallengeResultDto>> Handle(SubmitChallengeCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify challenge exists and is active
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(request.ChallengeId, cancellationToken);
        if (challenge == null)
            return Result<ChallengeResultDto>.Failure("Challenge not found.");

        if (!challenge.IsActive)
            return Result<ChallengeResultDto>.Failure("Challenge is inactive.");

        // 2. Verify campaign is active
        var campaign = await _unitOfWork.Campaigns.GetAsync(challenge.CampaignId, cancellationToken);
        if (campaign == null)
            return Result<ChallengeResultDto>.Failure("Campaign not found.");

        if (!campaign.IsActive)
            return Result<ChallengeResultDto>.Failure("Campaign is inactive.");

        // 3. Deserialize per-challenge anti-fraud config (falls back to defaults if null/invalid)
        var antiFraudConfig = DeserializeAntiFraudConfig(challenge.AntiFraudConfigJson);

        // 4. Build anti-fraud context
        var antiFraudContext = new AntiFraudContext
        {
            UserId = request.UserId,
            ChallengeId = request.ChallengeId,
            CampaignId = challenge.CampaignId,
            ChallengeType = challenge.Type,
            RemoteIp = request.RemoteIp,
            Inputs = request.Inputs,
            Config = antiFraudConfig,
            CampaignStartDate = campaign.StartDate,
            CampaignEndDate = campaign.EndDate
        };

        // 5. Run anti-fraud pipeline BEFORE executing the dynamic strategy
        try
        {
            await _antiFraudPipeline.RunAsync(antiFraudContext, cancellationToken);
        }
        catch (AntiFraudException ex)
        {
            // Record the failed attempt if tracking is enabled for this module
            if (ShouldTrackFailedAttempt(challenge.Type, antiFraudConfig))
            {
                await RecordFailedAttemptAsync(request, challenge, ex, cancellationToken);
            }
            return Result<ChallengeResultDto>.Failure($"[{ex.RuleCode}] {ex.Message}");
        }

        // 6. Check existing progress (already completed)
        var progress = await _unitOfWork.UserChallengeProgresses.GetByUserAndChallengeAsync(
            request.UserId, request.ChallengeId, cancellationToken);

        if (progress != null && progress.Status == ChallengeStatus.Completed)
            return Result<ChallengeResultDto>.Failure("You have already completed this challenge.");

        // 7. Process dynamic execution via service/strategy
        var dynamicResult = await _dynamicService.ProcessDynamicAsync(
            request.ChallengeId, request.UserId, request.Inputs, cancellationToken);

        // Extract score from payload
        int score = 0;
        if (dynamicResult.Payload is int intScore)
            score = intScore;
        else if (dynamicResult.Payload is IDictionary<string, object> dict
            && dict.TryGetValue("Score", out var scoreVal)
            && scoreVal is int dictScore)
            score = dictScore;

        // Rally submissions are async — the challenge stays Pending until a winner is selected.
        // Detect Rally by the typed payload marker returned by RallyStrategy.
        bool isRallySubmission = dynamicResult.Payload is RallySubmissionPayload;

        // 8. Update or create user progress
        var now = DateTime.UtcNow;
        // For Rally, status = Pending (not Completed) — prize fires on winner selection.
        var progressStatus = isRallySubmission
            ? ChallengeStatus.Pending
            : (dynamicResult.Success ? ChallengeStatus.Completed : ChallengeStatus.Failed);

        if (progress == null)
        {
            progress = new UserChallengeProgress
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                DynamicChallengeId = request.ChallengeId,
                Attempts = 1,
                Score = score,
                Status = progressStatus,
                CompletedAt = (!isRallySubmission && dynamicResult.Success) ? now : null
            };
            await _unitOfWork.UserChallengeProgresses.AddAsync(progress, cancellationToken);
        }
        else
        {
            progress.Attempts++;
            progress.Score = score;
            progress.Status = progressStatus;
            progress.CompletedAt = (!isRallySubmission && dynamicResult.Success) ? now : null;
            await _unitOfWork.UserChallengeProgresses.UpdateAsync(progress, cancellationToken);
        }

        // ─── Universal Position Win Evaluation ──────────────────────────────────────
        // Runs for ALL challenge types (Code, Trivia, Survey, Rally).
        // Position is isolated per DynamicChallengeId — no mixing across types.
        var prizes = await _unitOfWork.Prizes.GetByChallengeIdAsync(request.ChallengeId, cancellationToken);
        var posResult = await _positionWinService.EvaluateAsync(
            request.ChallengeId,
            challenge.PositionWinConfigJson,
            prizes.ToList(),
            cancellationToken);

        // If winner: reserve the positional prize in this submission's progress
        if (posResult.IsWinner && posResult.ReservedPrizeId.HasValue)
            progress.ReservedPrizeId = posResult.ReservedPrizeId;

        // Merge position win data into the result payload
        var mergedPayload = new Dictionary<string, object>();
        if (dynamicResult.Payload is IDictionary<string, object> strategyDict)
            foreach (var kv in strategyDict)
                mergedPayload[kv.Key] = kv.Value;
        if (posResult.IsEnabled)
        {
            mergedPayload["PositionBased"] = true;
            mergedPayload["Position"]      = posResult.Position;
            mergedPayload["IsWinner"]      = posResult.IsWinner;
            if (posResult.PrizeTier != null)
                mergedPayload["PrizeTier"] = posResult.PrizeTier;
        }
        var finalPayload = mergedPayload.Count > 0 ? (object)mergedPayload : dynamicResult.Payload;
        // ────────────────────────────────────────────────────────────────────────────

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 9. Publish ChallengeCompletedEvent ONLY for sync dynamics (Trivia, Survey, Code).
        //    Rally fires this event later via RallyWinnerSelectedEventHandler.
        //    Position-based: event always fires (handler filters positional prizes).
        if (dynamicResult.Success && !isRallySubmission)
        {
            var completedEvent = new ChallengeCompletedEvent(request.UserId, request.ChallengeId, progress.Id);
            await _publisher.Publish(completedEvent, cancellationToken);
        }

        // 10. Get awarded prizes for response DTO
        var awardedPrizeNames = new List<string>();
        int totalPointsAwarded = 0;
        if (dynamicResult.Success)
        {
            var userPrizes = await _unitOfWork.UserPrizes.GetByUserIdAsync(request.UserId, cancellationToken);
            foreach (var up in userPrizes)
            {
                if (up.Prize.DynamicChallengeId == request.ChallengeId
                    && up.AwardedAt >= now.AddSeconds(-10))
                {
                    awardedPrizeNames.Add($"{up.Prize.Name} (Code: {up.Code})");
                    totalPointsAwarded += up.PointsAwarded;
                }
            }
        }

        var resultDto = new ChallengeResultDto
        {
            Success     = dynamicResult.Success,
            Message     = dynamicResult.Message,
            Payload     = finalPayload,
            AwardedPrizeNames = awardedPrizeNames,
            PointsAwarded     = totalPointsAwarded,
            // Position-based: only chain to NextChallenge when user won.
            // Non-position: chain on success as before.
            NextChallengeId = posResult.IsEnabled
                ? (posResult.IsWinner ? challenge.NextChallengeId : null)
                : (dynamicResult.Success ? challenge.NextChallengeId : null)
        };

        return Result<ChallengeResultDto>.Success(resultDto);
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    private static AntiFraudCampaignConfig DeserializeAntiFraudConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new AntiFraudCampaignConfig();
        try
        {
            return JsonSerializer.Deserialize<AntiFraudCampaignConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new AntiFraudCampaignConfig();
        }
        catch
        {
            return new AntiFraudCampaignConfig();
        }
    }

    private static bool ShouldTrackFailedAttempt(DynamicType type, AntiFraudCampaignConfig cfg) =>
        type switch
        {
            DynamicType.Code => cfg.Code.TrackFailedAttempts,
            _ => false // Trivia and Encuesta don't track failed attempts by default
        };

    private async Task RecordFailedAttemptAsync(
        SubmitChallengeCommand request,
        DynamicChallenge challenge,
        AntiFraudException ex,
        CancellationToken cancellationToken)
    {
        try
        {
            var attempt = new FailedAttempt
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                ChallengeId = request.ChallengeId,
                CampaignId = challenge.CampaignId,
                SubmittedValue = request.Inputs.TryGetValue("code", out var c) ? c : null,
                RemoteIp = request.RemoteIp,
                RuleCode = ex.RuleCode,
                Reason = ex.Message,
                AttemptedAt = DateTime.UtcNow
            };
            await _unitOfWork.FailedAttempts.AddAsync(attempt, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Recording the attempt must never cause the request to fail with an unexpected error
        }
    }
}
