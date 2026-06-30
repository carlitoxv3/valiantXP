using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Rally.Queries;

/// <summary>
/// Returns the active Rally challenge info and its current state summary for a given campaign.
/// Mirrors PromoHub's GET /rally/active and GET /rally/get?id endpoints.
/// </summary>
public sealed record GetRallyInfoQuery(Guid ChallengeId) : IRequest<Result<RallyInfoDto>>;

public sealed class GetRallyInfoQueryHandler
    : IRequestHandler<GetRallyInfoQuery, Result<RallyInfoDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRallySubmissionRepository _submissionRepo;

    public GetRallyInfoQueryHandler(
        IUnitOfWork unitOfWork,
        IRallySubmissionRepository submissionRepo)
    {
        _unitOfWork = unitOfWork;
        _submissionRepo = submissionRepo;
    }

    public async Task<Result<RallyInfoDto>> Handle(GetRallyInfoQuery request, CancellationToken ct)
    {
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(request.ChallengeId, ct);
        if (challenge is null)
            return Result<RallyInfoDto>.Failure("Rally challenge not found.");

        if (challenge.Type != DynamicType.Rally)
            return Result<RallyInfoDto>.Failure("Challenge is not a Rally type.");

        var campaign = await _unitOfWork.Campaigns.GetAsync(challenge.CampaignId, ct);

        // Submission stats
        var allApproved = await _submissionRepo.GetApprovedAsync(request.ChallengeId, ct);
        var allWinners  = await _submissionRepo.GetWinnersAsync(request.ChallengeId, ct);

        // Parse rally config from ConfigurationJson
        var rallyConfig = ParseRallyType(challenge.ConfigurationJson);

        return Result<RallyInfoDto>.Success(new RallyInfoDto
        {
            ChallengeId        = challenge.Id,
            CampaignId         = challenge.CampaignId,
            Name               = challenge.Name,
            RallyType          = rallyConfig,
            IsActive           = challenge.IsActive,
            CampaignStartDate  = campaign?.StartDate,
            CampaignEndDate    = campaign?.EndDate,
            TotalApproved      = allApproved.Count,
            TotalWinners       = allWinners.Count,
            HasWinners         = allWinners.Count > 0,
            ConfigurationJson  = challenge.ConfigurationJson
        });
    }

    private static string ParseRallyType(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "Photo";
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rallyType", out var prop))
                return prop.GetString() ?? "Photo";
        }
        catch { }
        return "Photo";
    }
}

