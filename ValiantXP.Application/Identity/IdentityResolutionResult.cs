using System;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Application.Identity;

public enum IdentityResolutionStatus
{
    Resolved,       // User found or created — JWT can be issued
    LinkingRequired // Unverified email matches existing account — user must confirm
}

public class IdentityResolutionResult
{
    public IdentityResolutionStatus Status { get; init; }
    public User? User { get; init; }
    public string? PendingLinkToken { get; init; }  // only when LinkingRequired
    public string? SuggestedProvider { get; init; } // only when LinkingRequired

    public static IdentityResolutionResult Resolved(User user)
        => new() { Status = IdentityResolutionStatus.Resolved, User = user };

    public static IdentityResolutionResult RequiresLinking(string pendingLinkToken, string suggestedProvider)
        => new()
        {
            Status = IdentityResolutionStatus.LinkingRequired,
            PendingLinkToken = pendingLinkToken,
            SuggestedProvider = suggestedProvider
        };
}
