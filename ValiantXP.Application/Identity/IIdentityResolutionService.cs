using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValiantXP.Application.Identity;

public interface IIdentityResolutionService
{
    Task<IdentityResolutionResult> ResolveAsync(
        ExternalIdentityClaims claims,
        CancellationToken ct = default);

    Task<bool> ConfirmLinkAsync(
        string pendingLinkToken,
        Guid authenticatedUserId,
        CancellationToken ct = default);
}
