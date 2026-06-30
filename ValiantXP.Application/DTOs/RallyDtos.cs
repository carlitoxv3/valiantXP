using ValiantXP.Domain.Enums;

namespace ValiantXP.Application.DTOs;

// ─── Submit (via existing SubmitChallengeCommand, enriched fields) ─────────────

/// <summary>
/// Response DTO for a Rally submission via POST /api/dynamics/{id}/submit.
/// Embedded inside ChallengeResultDto.Payload when DynamicType = Rally.
/// </summary>
public sealed class RallySubmissionResultDto
{
    public Guid SubmissionId { get; init; }
    public string SubmissionCode { get; init; } = string.Empty;
    public string SubmissionStatus { get; init; } = "PendingModeration";
    public string Message { get; init; } = string.Empty;
}

// ─── Gallery ──────────────────────────────────────────────────────────────────

/// <summary>Request model for the Rally submissions gallery endpoint.</summary>
public sealed class GetRallySubmissionsRequestDto
{
    /// <summary>Filter by status. Null = all approved submissions (gallery default).</summary>
    public string? Status { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>Public view of a single Rally submission (gallery card).</summary>
public sealed class RallySubmissionDto
{
    public Guid Id { get; init; }
    public string SubmissionCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RallyType { get; init; } = string.Empty;
    public bool IsWinner { get; init; }
    public string? MediaUrl { get; init; }
    public string? TextContent { get; init; }
    public int VoteCount { get; init; }
    public string? SubChallengeTag { get; init; }
    public DateTime SubmittedAt { get; init; }

    /// <summary>User display name (anonymized for privacy — first name + last initial).</summary>
    public string? UserDisplayName { get; init; }
}

// ─── Vote ─────────────────────────────────────────────────────────────────────

/// <summary>Response after casting a vote on a Rally submission.</summary>
public sealed class VoteResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int NewVoteCount { get; init; }
}

// ─── Moderation ───────────────────────────────────────────────────────────────

/// <summary>Request body for moderating a Rally submission (admin).</summary>
public sealed class ModerateSubmissionRequestDto
{
    public string Decision { get; init; } = string.Empty; // "Approved" | "Rejected" | "Banned"
    public string? Notes { get; init; }
}

/// <summary>Response after a moderation action.</summary>
public sealed class ModerationResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
}

// ─── Winner selection ─────────────────────────────────────────────────────────

/// <summary>Request body for manual winner selection (admin).</summary>
public sealed class SelectWinnersRequestDto
{
    /// <summary>List of submission IDs to mark as winners.</summary>
    public IList<Guid> WinnerSubmissionIds { get; init; } = new List<Guid>();
}

/// <summary>Response after winner selection.</summary>
public sealed class WinnerSelectionResultDto
{
    public bool Success { get; init; }
    public int WinnersSelected { get; init; }
    public IList<string> WinnerCodes { get; init; } = new List<string>();
    public string Message { get; init; } = string.Empty;
}

// ─── Rally info (GET /api/rally/{id}) ────────────────────────────────────────

/// <summary>
/// Rally challenge metadata + live stats.
/// Combines PromoHub's /rally/active + /rally/get?id into one response.
/// </summary>
public sealed class RallyInfoDto
{
    public Guid ChallengeId { get; init; }
    public Guid CampaignId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string RallyType { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? CampaignStartDate { get; init; }
    public DateTime? CampaignEndDate { get; init; }
    public int TotalApproved { get; init; }
    public int TotalWinners { get; init; }
    public bool HasWinners { get; init; }
    /// <summary>Raw ConfigurationJson for client-side rendering (sub-challenges, etc.).</summary>
    public string? ConfigurationJson { get; init; }
}

// ─── Moderation item detail (admin) ──────────────────────────────────────────

/// <summary>
/// Full detail of a single Rally submission for admin moderation review.
/// Includes ticket data and moderation audit trail.
/// Mirrors PromoHub Admin's GET /multimedia/moderation/{id}.
/// </summary>
public sealed class RallyModerationItemDto
{
    public Guid Id { get; init; }
    public string SubmissionCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RallyType { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string? MediaUrl { get; init; }
    public string? TextContent { get; init; }
    /// <summary>JSON with ticket number, point of sale, line items (for Ticket-type rallies).</summary>
    public string? TicketDataJson { get; init; }
    public string? SubChallengeTag { get; init; }
    public int VoteCount { get; init; }
    public DateTime SubmittedAt { get; init; }
    public DateTime? ModeratedAt { get; init; }
    public Guid? ModeratedByUserId { get; init; }
    public string? ModerationNotes { get; init; }
    public string? RemoteIp { get; init; }
}

// ─── Pre-validate access ─────────────────────────────────────────────────────

/// <summary>
/// Response from POST /api/rally/{id}/validate.
/// Tells the client if the user can submit, how many submissions remain,
/// and which sub-challenge they should complete next.
/// Mirrors PromoHub's /rallies/validate (Chatbot) and /rally/{id}/validate (User API).
/// </summary>
public sealed class RallyAccessValidationDto
{
    public bool CanSubmit { get; init; }
    /// <summary>Null = unlimited. Number of remaining submissions in the current period.</summary>
    public int? RemainingSubmissions { get; init; }
    public int PeriodHours { get; init; }
    public SubChallengeDto? AvailableSubChallenge { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>A single sub-challenge definition returned during Rally validation.</summary>
public sealed class SubChallengeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
