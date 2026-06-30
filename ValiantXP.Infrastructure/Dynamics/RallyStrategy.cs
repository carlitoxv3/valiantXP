using System.Text.Json;
using ValiantXP.Application.Features.Dynamics;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Dynamics;

/// <summary>
/// Strategy for the Rally (UGC competition) dynamic.
///
/// Key design principle:
///   This strategy does NOT immediately complete the challenge.
///   It validates the submission type/format, persists the RallySubmission,
///   and returns Success=true with SubmissionStatus=PendingModeration.
///
///   The challenge is only "Completed" (for prize purposes) when
///   RallyWinnerSelectedEvent fires → RallyWinnerSelectedEventHandler →
///   ChallengeCompletedEvent → ChallengeCompletedEventHandler.
///
/// Mirrors PromoHub's RallyService.Update() flow without the external AI/OCR calls.
/// </summary>
public sealed class RallyStrategy : IDynamicStrategy
{
    public string DynamicType => Domain.Enums.DynamicType.Rally.ToString();

    private readonly IUnitOfWork _unitOfWork;
    private readonly IRallySubmissionRepository _submissionRepo;

    public RallyStrategy(IUnitOfWork unitOfWork, IRallySubmissionRepository submissionRepo)
    {
        _unitOfWork = unitOfWork;
        _submissionRepo = submissionRepo;
    }

    public async Task<DynamicResult> ExecuteAsync(DynamicContext context, CancellationToken cancellationToken)
    {
        // 1. Load challenge to get Rally configuration
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(context.DynamicId, cancellationToken);
        if (challenge is null)
            return new DynamicResult { Success = false, Message = "Challenge not found." };

        // 2. Parse RallyConfig from ConfigurationJson
        var rallyConfig = ParseRallyConfig(challenge.ConfigurationJson);

        // 3. Validate required inputs based on RallyType
        var rallyType = rallyConfig.RallyType;
        var validationError = ValidateInputs(context.Inputs, rallyType);
        if (validationError is not null)
            return new DynamicResult { Success = false, Message = validationError };

        // 4. Generate unique submission code (RALLY-[6 chars])
        var code = GenerateSubmissionCode();

        // 5. Build RallySubmission entity
        var submission = new RallySubmission
        {
            Id = Guid.NewGuid(),
            DynamicChallengeId = context.DynamicId,
            UserId = context.UserId,
            SubmissionCode = code,
            RallyType = rallyType,
            Status = RallySubmissionStatus.PendingModeration,
            IsWinner = false,
            MediaUrl = context.Inputs.GetValueOrDefault("mediaUrl"),
            TextContent = context.Inputs.GetValueOrDefault("textContent"),
            TicketDataJson = context.Inputs.GetValueOrDefault("ticketData"),
            SubChallengeTag = context.Inputs.GetValueOrDefault("subChallengeTag"),
            RemoteIp = context.Inputs.GetValueOrDefault("remoteIp"),
            SubmittedAt = DateTime.UtcNow
        };

        // 6. Persist the submission
        await _submissionRepo.AddAsync(submission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Return success with PendingModeration — ChallengeCompletedEvent does NOT fire now
        //    It will fire only when SelectRallyWinnersCommand marks this user as a winner.
        return new DynamicResult
        {
            Success = true,
            Message = "Your submission has been received and is pending review. " +
                      "You will be notified once it has been approved.",
            Payload = new RallySubmissionPayload(
                submission.Id,
                submission.SubmissionCode,
                submission.Status.ToString())
        };
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string? ValidateInputs(IReadOnlyDictionary<string, string> inputs, RallyType type)
    {
        return type switch
        {
            RallyType.Photo or RallyType.Social or RallyType.Card =>
                !inputs.TryGetValue("mediaUrl", out var url) || string.IsNullOrWhiteSpace(url)
                    ? $"A media URL is required for {type} rally submissions."
                    : null,

            RallyType.Ticket or RallyType.Consumption =>
                !inputs.TryGetValue("ticketData", out var td) || string.IsNullOrWhiteSpace(td)
                    ? $"Ticket data JSON is required for {type} rally submissions."
                    : null,

            RallyType.Story =>
                !inputs.TryGetValue("textContent", out var tc) || string.IsNullOrWhiteSpace(tc)
                    ? "Text content is required for Story rally submissions."
                    : null,

            _ => null
        };
    }

    private static string GenerateSubmissionCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Unambiguous chars
        var rng = new Random();
        var suffix = new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        return $"RALLY-{suffix}";
    }

    private static RallyConfigSchema ParseRallyConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new RallyConfigSchema();
        try
        {
            return JsonSerializer.Deserialize<RallyConfigSchema>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new RallyConfigSchema();
        }
        catch { return new RallyConfigSchema(); }
    }

    /// <summary>Schema for ConfigurationJson when DynamicType = Rally.</summary>
    private sealed class RallyConfigSchema
    {
        public RallyType RallyType { get; init; } = RallyType.Photo;
        public int MaxSubmissionsPerUserPerPeriod { get; init; } = 1;
        public int PeriodHours { get; init; } = 24;
        public bool VotingEnabled { get; init; } = true;
        public bool RequiresModeration { get; init; } = true;
        public string WinnerSelectionMode { get; init; } = "ByAdmin";
        public int NumberOfWinners { get; init; } = 1;
        // Sub-challenges (PromoHub pattern)
        public IList<SubChallengeSchema> SubChallenges { get; init; } = new List<SubChallengeSchema>();
    }

    private sealed class SubChallengeSchema
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public bool Status { get; init; } = true;
    }
}
