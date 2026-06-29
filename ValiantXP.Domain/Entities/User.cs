using System;
using System.Collections.Generic;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

public class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? MfaSecret { get; set; }
    public bool IsMfaEnabled { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
