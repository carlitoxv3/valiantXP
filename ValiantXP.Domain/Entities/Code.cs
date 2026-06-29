using System;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

public class Code : Entity
{
    public string CodeNumber { get; set; } = string.Empty;
    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? RemoteIP { get; set; }
}
