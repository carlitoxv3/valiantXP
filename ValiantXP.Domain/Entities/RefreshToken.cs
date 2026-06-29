using System;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

public class RefreshToken : Entity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
