using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Interfaces;

public interface IUserIdentityRepository
{
    Task<UserIdentity?> FindAsync(IdentityProvider provider, string externalId, CancellationToken ct = default);
    Task<IReadOnlyList<UserIdentity>> FindByEmailClaimAsync(string emailClaim, bool onlyVerified = true, CancellationToken ct = default);
    Task<IReadOnlyList<UserIdentity>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(UserIdentity identity, CancellationToken ct = default);
    Task UpdateAsync(UserIdentity identity, CancellationToken ct = default);
}
