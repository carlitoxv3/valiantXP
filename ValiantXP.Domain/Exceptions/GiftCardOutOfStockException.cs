using System;

namespace ValiantXP.Domain.Exceptions;

/// <summary>Thrown when TryAssignFromPool finds no available codes in the pool.</summary>
public class GiftCardOutOfStockException : Exception
{
    public Guid ProviderId { get; }

    public GiftCardOutOfStockException(Guid providerId)
        : base($"GiftCard pool is out of stock for provider {providerId}.")
    {
        ProviderId = providerId;
    }
}
