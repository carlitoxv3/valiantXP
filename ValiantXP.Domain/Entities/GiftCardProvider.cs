using System;
using System.Collections.Generic;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

/// <summary>Groups a pool of GiftCard codes under a named provider (e.g. Amazon, Sodexo).</summary>
public class GiftCardProvider : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? InstructiveUrl { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Optional: scoped to a specific campaign. Null = global provider.</summary>
    public Guid? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public ICollection<GiftCard> GiftCards { get; set; } = new List<GiftCard>();

    /// <summary>Prizes that use this provider as their delivery mechanism.</summary>
    public ICollection<Prize> Prizes { get; set; } = new List<Prize>();
}
