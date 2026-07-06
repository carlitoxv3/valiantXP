using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValiantXP.Domain.Interfaces;

/// <summary>
/// Abstraction over external or internal GiftCard providers.
/// Maps to PromoHub's IGiftCardService + dbo.SetGiftCard SP.
/// Implement for each provider (e.g., InternalGiftCardProvider, StoreXProvider).
/// </summary>
public interface IGiftCardProvider
{
    string ProviderName { get; }

    Task<GiftCardResult> IssueGiftCardAsync(
        Guid userId,
        Guid prizeId,
        CancellationToken ct = default);
}

/// <summary>Result from a GiftCard issuance attempt.</summary>
public record GiftCardResult(bool Success, string? Code, string? Message);
