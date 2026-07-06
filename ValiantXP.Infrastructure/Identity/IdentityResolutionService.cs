using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ValiantXP.Application.Identity;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Identity;

public class IdentityResolutionService : IIdentityResolutionService
{
    private readonly IUnitOfWork _uow;
    private readonly IPendingLinkService _pendingLinkService;
    private readonly ILogger<IdentityResolutionService> _logger;

    public IdentityResolutionService(
        IUnitOfWork uow,
        IPendingLinkService pendingLinkService,
        ILogger<IdentityResolutionService> logger)
    {
        _uow = uow;
        _pendingLinkService = pendingLinkService;
        _logger = logger;
    }

    public async Task<IdentityResolutionResult> ResolveAsync(
        ExternalIdentityClaims claims,
        CancellationToken ct = default)
    {
        // STEP 1 — Direct lookup by (Provider, ExternalId)
        var existing = await _uow.UserIdentities.FindAsync(claims.Provider, claims.ExternalId, ct);
        if (existing != null)
        {
            existing.LastSeenAt = DateTime.UtcNow;
            await _uow.UserIdentities.UpdateAsync(existing, ct);
            await _uow.SaveChangesAsync(ct);
            var existingUser = await GetUserAsync(existing.UserId, ct);
            return IdentityResolutionResult.Resolved(existingUser);
        }

        // STEP 2 — Auto-merge by verified email (BOTH sides must be verified)
        if (claims.IsEmailVerified && !string.IsNullOrEmpty(claims.EmailClaim))
        {
            var matchingIdentities = await _uow.UserIdentities
                .FindByEmailClaimAsync(claims.EmailClaim, onlyVerified: true, ct);

            if (matchingIdentities.Count == 1)
            {
                // Auto-merge — both sides verified
                _logger.LogInformation(
                    "Auto-merging identity {Provider}:{ExternalId} with existing user {UserId} via verified email {Email}",
                    claims.Provider, claims.ExternalId, matchingIdentities[0].UserId, claims.EmailClaim);

                await LinkIdentityAsync(matchingIdentities[0].UserId, claims, ct);
                await _uow.SaveChangesAsync(ct);
                var mergedUser = await GetUserAsync(matchingIdentities[0].UserId, ct);
                return IdentityResolutionResult.Resolved(mergedUser);
            }

            if (matchingIdentities.Count > 1)
            {
                // Conflict — multiple verified users with same email (shouldn't happen)
                _logger.LogError(
                    "Identity conflict: multiple verified users share email {Email}",
                    claims.EmailClaim);
                // Fall through to create new user (safe degradation)
            }
        }

        // STEP 3 — Unverified email matches existing account → prompt user to confirm
        if (!claims.IsEmailVerified && !string.IsNullOrEmpty(claims.EmailClaim))
        {
            var matchingIdentities = await _uow.UserIdentities
                .FindByEmailClaimAsync(claims.EmailClaim, onlyVerified: true, ct);

            if (matchingIdentities.Count == 1)
            {
                var pendingToken = await _pendingLinkService.CreateAsync(claims, ct);
                return IdentityResolutionResult.RequiresLinking(
                    pendingToken,
                    matchingIdentities[0].Provider.ToString());
            }
        }

        // STEP 4 — No match → create new user + link identity
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = claims.EmailClaim ?? string.Empty,
            UserName = claims.EmailClaim?.Split('@')[0] ?? $"user_{Guid.NewGuid().ToString("N")[..8]}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(newUser, ct);
        await LinkIdentityAsync(newUser.Id, claims, ct);
        await _uow.SaveChangesAsync(ct);

        return IdentityResolutionResult.Resolved(newUser);
    }

    public async Task<bool> ConfirmLinkAsync(
        string pendingLinkToken,
        Guid authenticatedUserId,
        CancellationToken ct = default)
    {
        var pendingClaims = await _pendingLinkService.ValidateAndConsumeAsync(pendingLinkToken, ct);
        if (pendingClaims is null) return false;

        await LinkIdentityAsync(authenticatedUserId, pendingClaims, ct);
        await _uow.SaveChangesAsync(ct);
        return true;
    }

    private async Task LinkIdentityAsync(
        Guid userId,
        ExternalIdentityClaims claims,
        CancellationToken ct)
    {
        var identity = new UserIdentity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = claims.Provider,
            ExternalId = claims.ExternalId,
            EmailClaim = claims.EmailClaim,
            IsEmailVerified = claims.IsEmailVerified,
            IsVerified = true,
            ClaimsJson = claims.ClaimsJson,
            LinkedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IsActive = true
        };
        await _uow.UserIdentities.AddAsync(identity, ct);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _uow.Users.GetAsync(userId, ct);
        return user ?? throw new InvalidOperationException($"User {userId} not found after identity link");
    }
}
