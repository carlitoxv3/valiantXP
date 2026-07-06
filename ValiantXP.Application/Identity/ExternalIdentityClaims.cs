using ValiantXP.Domain.Enums;

namespace ValiantXP.Application.Identity;

public record ExternalIdentityClaims(
    IdentityProvider Provider,
    string ExternalId,
    string? EmailClaim,
    bool IsEmailVerified,
    string? ClaimsJson = null
);
