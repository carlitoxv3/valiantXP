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
