using MediatR;
using System.Text.Json;
using ValiantXP.Application.AntiFraud;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.Commands;

/// <summary>
/// Pre-validates a user's access to a Rally before they fill out the submission form.
/// Returns the available sub-challenge (if any) and submission limits.
/// Does NOT create any submission record — purely a read + validation operation.
///
/// Mirrors PromoHub's GET /rallies/validate endpoint (Chatbot API) and
/// POST /rally/{rallyId}/validate (User API).
///
/// Flow: Client calls this first → displays the sub-challenge instructions →
///       User prepares media → Client calls POST /api/dynamics/{id}/submit.
/// </summary>
public sealed record ValidateRallyAccessCommand(
    Guid ChallengeId,
    Guid UserId,
    string? RemoteIp = null
) : IRequest<Result<RallyAccessValidationDto>>;

public sealed class ValidateRallyAccessCommandHandler
    : IRequestHandler<ValidateRallyAccessCommand, Result<RallyAccessValidationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IAntiFraudPipeline _antiFraudPipeline;

    public ValidateRallyAccessCommandHandler(
        IUnitOfWork unitOfWork,
        IRallySubmissionRepository submissionRepo,
        IAntiFraudPipeline antiFraudPipeline)
    {
        _unitOfWork = unitOfWork;
        _submissionRepo = submissionRepo;
        _antiFraudPipeline = antiFraudPipeline;
    }

    public async Task<Result<RallyAccessValidationDto>> Handle(
        ValidateRallyAccessCommand request, CancellationToken ct)
    {
        // 1. Load challenge
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(request.ChallengeId, ct);
        if (challenge is null || !challenge.IsActive)
            return Result<RallyAccessValidationDto>.Failure("Rally is not active.");

        if (challenge.Type != DynamicType.Rally)
            return Result<RallyAccessValidationDto>.Failure("Challenge is not a Rally type.");

        var campaign = await _unitOfWork.Campaigns.GetAsync(challenge.CampaignId, ct);
        if (campaign is null || !campaign.IsActive)
            return Result<RallyAccessValidationDto>.Failure("Campaign is not active.");

        // 2. Deserialize anti-fraud config
        var antiFraudConfig = DeserializeAntiFraudConfig(challenge.AntiFraudConfigJson);

        // 3. Run anti-fraud pipeline with no inputs (just frequency/window checks)
        var antiFraudContext = new AntiFraudContext
        {
            UserId = request.UserId,
            ChallengeId = request.ChallengeId,
            CampaignId = challenge.CampaignId,
            ChallengeType = DynamicType.Rally,
            RemoteIp = request.RemoteIp,
            Inputs = new Dictionary<string, string>(),
            Config = antiFraudConfig,
            CampaignStartDate = campaign.StartDate,
            CampaignEndDate = campaign.EndDate
        };

        try
        {
            await _antiFraudPipeline.RunAsync(antiFraudContext, ct);
        }
        catch (Domain.Exceptions.AntiFraudException ex)
        {
            return Result<RallyAccessValidationDto>.Failure($"[{ex.RuleCode}] {ex.Message}");
        }

        // 4. Determine remaining submissions for this period
        var cfg = antiFraudConfig.Rally;
        var windowStart = DateTime.UtcNow.AddHours(-cfg.PeriodHours);
        var usedCount = await _submissionRepo.GetSubmissionCountAsync(
            request.UserId, request.ChallengeId, windowStart, ct);
        var remaining = cfg.MaxSubmissionsPerUserPerPeriod > 0
            ? Math.Max(0, cfg.MaxSubmissionsPerUserPerPeriod - usedCount)
            : int.MaxValue;

        // 5. Find available sub-challenge (mirrors PromoHub's GetRallyChallengeAsync)
        var availableSubChallenge = await FindAvailableSubChallengeAsync(
            request.UserId, request.ChallengeId, challenge.ConfigurationJson, ct);

        return Result<RallyAccessValidationDto>.Success(new RallyAccessValidationDto
        {
            CanSubmit = remaining > 0,
            RemainingSubmissions = remaining == int.MaxValue ? null : remaining,
            PeriodHours = cfg.PeriodHours,
            AvailableSubChallenge = availableSubChallenge,
            Message = remaining > 0
                ? "You are eligible to submit."
                : $"Submission limit reached. Next available in {cfg.PeriodHours}h window."
        });
    }

    // ─── Sub-challenge selection (mirrors PromoHub's GetRallyChallengeAvaible) ──

    private async Task<SubChallengeDto?> FindAvailableSubChallengeAsync(
        Guid userId, Guid challengeId, string? configJson, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            if (!doc.RootElement.TryGetProperty("subChallenges", out var subArr)
                || subArr.ValueKind != JsonValueKind.Array)
                return null;

            // Get already-used sub-challenge IDs from existing user submissions
            var userSubmissions = await _submissionRepo.GetByUserAsync(userId, challengeId, ct);
            var usedSubChallengeIds = userSubmissions
                .Where(s => !string.IsNullOrWhiteSpace(s.SubChallengeTag))
                .Select(s => ExtractSubChallengeId(s.SubChallengeTag!))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            // Find first active sub-challenge not yet used by this user
            foreach (var sub in subArr.EnumerateArray())
            {
                if (!sub.TryGetProperty("id", out var idProp)) continue;
                var id = idProp.GetInt32();
                if (usedSubChallengeIds.Contains(id)) continue;
                if (sub.TryGetProperty("status", out var statusProp)
                    && statusProp.ValueKind == JsonValueKind.False)
                    continue;

                return new SubChallengeDto
                {
                    Id = id,
                    Name = sub.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    Description = sub.TryGetProperty("description", out var d) ? d.GetString() ?? "" : ""
                };
            }
        }
        catch { }
        return null; // No available sub-challenges
    }

    private static int? ExtractSubChallengeId(string tag)
    {
        try
        {
            using var doc = JsonDocument.Parse(tag);
            if (doc.RootElement.TryGetProperty("challengeId", out var prop))
                return prop.GetInt32();
        }
        catch { }
        return null;
    }

    private static AntiFraudCampaignConfig DeserializeAntiFraudConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<AntiFraudCampaignConfig>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return new(); }
    }
}

