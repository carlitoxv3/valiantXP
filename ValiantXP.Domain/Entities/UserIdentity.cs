using System;
using ValiantXP.Domain.Common;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Entities;

public class UserIdentity : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public IdentityProvider Provider { get; set; }
    public string ExternalId { get; set; } = string.Empty;

    public string? EmailClaim { get; set; }
    public bool IsEmailVerified { get; set; } = false;
    public bool IsVerified { get; set; } = false;
    public bool IsPrimary { get; set; } = false;

    public string? ClaimsJson { get; set; }

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAt { get; set; }

    // Soft unlink
    public bool IsActive { get; set; } = true;
    public DateTime? UnlinkedAt { get; set; }
}
