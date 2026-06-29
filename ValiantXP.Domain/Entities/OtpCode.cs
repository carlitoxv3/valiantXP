using System;
using ValiantXP.Domain.Common;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Entities;

public class OtpCode : Entity
{
    public string Target { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OtpChannel Channel { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; }
}
