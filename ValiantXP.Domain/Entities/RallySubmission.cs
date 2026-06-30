using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Entities;

/// <summary>
/// Represents a user's Rally submission — the core UGC artifact in a Rally dynamic.
/// Mirrors PromoHub's RallyMultimedia entity but adapted to Clean Architecture.
///
/// Lifecycle: PendingModeration → Approved/Rejected/Banned → (if winner) → Prize assigned.
/// </summary>
public class RallySubmission
{
    public Guid Id { get; set; }

    /// <summary>Reference to the Rally DynamicChallenge.</summary>
    public Guid DynamicChallengeId { get; set; }
    public DynamicChallenge DynamicChallenge { get; set; } = null!;

    /// <summary>Submitting user.</summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Unique, human-readable submission identifier.
    /// Format: RALLY-[6 alphanumeric chars]. Example: RALLY-XK9F3T.
    /// </summary>
    public string SubmissionCode { get; set; } = string.Empty;

    /// <summary>Rally type at time of submission (denormalized for query performance).</summary>
    public RallyType RallyType { get; set; }

    /// <summary>Current moderation status.</summary>
    public RallySubmissionStatus Status { get; set; } = RallySubmissionStatus.PendingModeration;

    /// <summary>True when this submission has been declared a winner.</summary>
    public bool IsWinner { get; set; } = false;

    // ─── Content Fields ───────────────────────────────────────────────────────

    /// <summary>
    /// URL of the uploaded media (image/video).
    /// Required for: Photo, Story, Social, Card.
    /// Client is responsible for uploading to storage and providing this URL.
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>Text content of the submission (title / caption / story body).</summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// JSON payload for Ticket and Consumption types.
    /// Schema: { nroTicket, pointOfSale, ticketDate, consumptionCenterId, lineItems: [{qty, brand, variant, points, total}] }
    /// </summary>
    public string? TicketDataJson { get; set; }

    /// <summary>
    /// Sub-challenge tag JSON (mirrors PromoHub's RallyMultimediaTag).
    /// Schema: { challengeId, internalId, consumptionCenterId }
    /// Populated when the rally has sub-challenges configured.
    /// </summary>
    public string? SubChallengeTag { get; set; }

    // ─── Audit Fields ─────────────────────────────────────────────────────────

    public string? RemoteIp { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModeratedAt { get; set; }
    public Guid? ModeratedByUserId { get; set; }
    public string? ModerationNotes { get; set; }

    // ─── Navigation ───────────────────────────────────────────────────────────

    public ICollection<RallySubmissionVote> Votes { get; set; } = new List<RallySubmissionVote>();
}
