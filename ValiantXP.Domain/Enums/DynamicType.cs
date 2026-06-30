namespace ValiantXP.Domain.Enums;

/// <summary>
/// Supported dynamic challenge types.
/// Each type maps to a concrete IDynamicStrategy implementation.
/// </summary>
public enum DynamicType
{
    /// <summary>Multiple-choice quiz. Score compared against correct answers in ConfigurationJson.</summary>
    Trivia = 1,

    /// <summary>Opinion survey. Always succeeds on submission. Answers stored for analytics.</summary>
    Survey = 2,

    /// <summary>Promo code redemption. Validates code exists and is unused, then marks it consumed.</summary>
    Code = 3,

    /// <summary>
    /// User-generated content competition. Users submit media (photo, ticket, story, etc.)
    /// which enters a moderation queue. Completion is async — winner selection fires the prize event.
    /// </summary>
    Rally = 4
}
