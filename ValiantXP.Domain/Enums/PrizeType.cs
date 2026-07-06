namespace ValiantXP.Domain.Enums;

public enum PrizeType
{
    Points = 1,
    Product = 2,
    // GiftCard = 3 — DEPRECATED. Use PrizeType.Product + Prize.GiftCardProviderId != null.
    // Will be fully removed after EF migration in Sprint 10 (GC-5).
    // [Obsolete("Use PrizeType.Product with Prize.GiftCardProviderId set instead.")]
    // GiftCard = 3,
    WheelChance = 4
}
