// Simple in-memory implementation (10-min TTL, one-time use)
// For production: replace with Redis or DB-backed implementation
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Identity;

namespace ValiantXP.Infrastructure.Identity;

public class InMemoryPendingLinkService : IPendingLinkService
{
    private readonly ConcurrentDictionary<string, (ExternalIdentityClaims Claims, DateTime ExpiresAt)> _store
        = new();

    public Task<string> CreateAsync(ExternalIdentityClaims claims, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N");
        _store[token] = (claims, DateTime.UtcNow.AddMinutes(10));
        return Task.FromResult(token);
    }

    public Task<ExternalIdentityClaims?> ValidateAndConsumeAsync(string token, CancellationToken ct = default)
    {
        if (_store.TryRemove(token, out var entry))
        {
            if (DateTime.UtcNow <= entry.ExpiresAt)
                return Task.FromResult<ExternalIdentityClaims?>(entry.Claims);
        }
        return Task.FromResult<ExternalIdentityClaims?>(null);
    }
}
