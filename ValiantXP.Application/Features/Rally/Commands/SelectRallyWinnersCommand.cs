using MediatR;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Events;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Rally.Commands;

/// <summary>
/// Selects winners from approved Rally submissions.
/// Supports all WinnerSelectionMode strategies: ByAdmin, ByVotes, ByLottery, ByTicketAmount.
/// Publishes RallyWinnerSelectedEvent → ChallengeCompletedEventHandler assigns prizes per winner.
/// </summary>
public sealed record SelectRallyWinnersCommand(
    Guid ChallengeId,
    Guid AdminUserId,
    /// <summary>Explicit winner IDs for ByAdmin mode. Ignored for other modes.</summary>
    IList<Guid>? ExplicitWinnerSubmissionIds = null
) : IRequest<Result<WinnerSelectionResultDto>>;

public sealed class SelectRallyWinnersCommandHandler
    : IRequestHandler<SelectRallyWinnersCommand, Result<WinnerSelectionResultDto>>
{
    private readonly IRallySubmissionRepository _submissionRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public SelectRallyWinnersCommandHandler(
        IRallySubmissionRepository submissionRepo,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _submissionRepo = submissionRepo;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<Result<WinnerSelectionResultDto>> Handle(
        SelectRallyWinnersCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load challenge to get configuration
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(request.ChallengeId, cancellationToken);
        if (challenge is null)
            return Result<WinnerSelectionResultDto>.Failure("Challenge not found.");

        // 2. Parse Rally config from AntiFraudConfigJson
        var antiFraudConfig = DeserializeAntiFraudConfig(challenge.AntiFraudConfigJson);
        var rallyConfig = antiFraudConfig.Rally;
        var mode = ParseWinnerSelectionMode(rallyConfig.WinnerSelectionMode);
        var numberOfWinners = rallyConfig.NumberOfWinners;

        // 3. Resolve winner submissions based on mode
        var approved = await _submissionRepo.GetApprovedAsync(request.ChallengeId, cancellationToken);
        if (!approved.Any())
            return Result<WinnerSelectionResultDto>.Failure("No approved submissions found for this challenge.");

        IList<Domain.Entities.RallySubmission> selectedSubmissions = mode switch
        {
            WinnerSelectionMode.ByAdmin => SelectByAdmin(approved, request.ExplicitWinnerSubmissionIds),
            WinnerSelectionMode.ByVotes => await SelectByVotesAsync(request.ChallengeId, numberOfWinners, cancellationToken),
            WinnerSelectionMode.ByLottery => SelectByLottery(approved, numberOfWinners),
            WinnerSelectionMode.ByTicketAmount => SelectByTicketAmount(approved, numberOfWinners),
            _ => SelectByAdmin(approved, request.ExplicitWinnerSubmissionIds)
        };

        if (!selectedSubmissions.Any())
            return Result<WinnerSelectionResultDto>.Failure("Could not select winners with the current configuration.");

        // 4. Mark selected submissions as winners
        var winnerCodes = new List<string>();
        var winnerUserIds = new List<Guid>();

        foreach (var sub in selectedSubmissions)
        {
            sub.IsWinner = true;
            await _submissionRepo.UpdateAsync(sub, cancellationToken);
            winnerCodes.Add(sub.SubmissionCode);
            winnerUserIds.Add(sub.UserId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Publish RallyWinnerSelectedEvent → triggers prize assignment per winner
        var winnerEvent = new RallyWinnerSelectedEvent(
            request.ChallengeId,
            challenge.CampaignId,
            selectedSubmissions.Select(s => s.Id).ToList(),
            winnerUserIds);

        await _publisher.Publish(winnerEvent, cancellationToken);

        return Result<WinnerSelectionResultDto>.Success(new WinnerSelectionResultDto
        {
            Success = true,
            WinnersSelected = selectedSubmissions.Count,
            WinnerCodes = winnerCodes,
            Message = $"{selectedSubmissions.Count} winner(s) selected via {mode} mode."
        });
    }

    // ─── Selection strategies ─────────────────────────────────────────────────

    private static IList<Domain.Entities.RallySubmission> SelectByAdmin(
        IList<Domain.Entities.RallySubmission> approved,
        IList<Guid>? explicitIds)
    {
        if (explicitIds is null || !explicitIds.Any())
            return new List<Domain.Entities.RallySubmission>();

        return approved.Where(s => explicitIds.Contains(s.Id)).ToList();
    }

    private async Task<IList<Domain.Entities.RallySubmission>> SelectByVotesAsync(
        Guid challengeId, int count, CancellationToken ct)
    {
        var ranked = await _submissionRepo.GetRankedByVotesAsync(challengeId, ct);
        return ranked.Take(count).ToList();
    }

    private static IList<Domain.Entities.RallySubmission> SelectByLottery(
        IList<Domain.Entities.RallySubmission> approved, int count)
    {
        var rng = new Random();
        return approved.OrderBy(_ => rng.Next()).Take(count).ToList();
    }

    private static IList<Domain.Entities.RallySubmission> SelectByTicketAmount(
        IList<Domain.Entities.RallySubmission> approved, int count)
    {
        // Sort by ticket total (parsed from TicketDataJson). Submissions without ticket data rank last.
        return approved
            .OrderByDescending(s => ParseTicketTotal(s.TicketDataJson))
            .Take(count)
            .ToList();
    }

    private static double ParseTicketTotal(string? ticketDataJson)
    {
        if (string.IsNullOrWhiteSpace(ticketDataJson)) return 0;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(ticketDataJson);
            if (doc.RootElement.TryGetProperty("total", out var total))
                return total.GetDouble();
        }
        catch { }
        return 0;
    }

    // ─── Config helpers ───────────────────────────────────────────────────────

    private static Domain.AntiFraud.AntiFraudCampaignConfig DeserializeAntiFraudConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Domain.AntiFraud.AntiFraudCampaignConfig>(
                json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new();
        }
        catch { return new(); }
    }

    private static WinnerSelectionMode ParseWinnerSelectionMode(string mode) =>
        Enum.TryParse<WinnerSelectionMode>(mode, ignoreCase: true, out var parsed)
            ? parsed
            : WinnerSelectionMode.ByAdmin;
}
