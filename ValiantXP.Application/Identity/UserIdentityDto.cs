using System;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Application.Identity;

public class UserIdentityDto
{
    public Guid Id { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? EmailClaim { get; init; }
    public bool IsEmailVerified { get; init; }
    public bool IsPrimary { get; init; }
    public DateTime LinkedAt { get; init; }
    public DateTime? LastSeenAt { get; init; }
}
