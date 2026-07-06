using System;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

/// <summary>
/// Audit ledger for every point credit/debit.
/// Mirrors PromoHub's ProfileMovements + InsertMovements SP.
/// Positive Points = credit, negative = debit (reserved for future use).
/// </summary>
public class UserPointMovement : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Points amount. Positive = credit.</summary>
    public int Points { get; set; }

    /// <summary>Source module: "Trivia", "Code", "Rally", "Survey", etc.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Human-readable description of the movement.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The challenge that triggered this movement (nullable for manual adjustments).</summary>
    public Guid? ChallengeId { get; set; }

    /// <summary>The prize that generated these points (nullable).</summary>
    public Guid? PrizeId { get; set; }

    /// <summary>When these points expire. Null = no expiry.</summary>
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
