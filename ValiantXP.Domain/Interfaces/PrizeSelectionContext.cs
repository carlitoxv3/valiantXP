using System;

namespace ValiantXP.Domain.Interfaces;

/// <summary>
/// Context object passed through the InstantWin selection pipeline.
/// Equivalent to PromoHub's ContentData — carries all information needed
/// for eligibility filters and award strategies.
/// </summary>
public class PrizeSelectionContext
{
    public Guid UserId { get; set; }
    public Guid ChallengeId { get; set; }

    /// <summary>Rally submission that triggered the award (enables ticket-line-item multiplier).</summary>
    public Guid? SubmissionId { get; set; }

    /// <summary>Evaluation timestamp — use this instead of DateTime.UtcNow for deterministic tests.</summary>
    public DateTime Now { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// JSON of ticket line items from a Rally RallySubmission.TicketDataJson.
    /// When present, PointsPrizeAwardStrategy multiplies points by sum of item quantities.
    /// Maps to the RallyMultimediaTicket OPENJSON logic in dbo.InstantWin_Save.
    /// </summary>
    public string? TicketLineItemsJson { get; set; }
}
