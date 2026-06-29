using System;
using System.Collections.Generic;
using ValiantXP.Domain.Common;

namespace ValiantXP.Domain.Entities;

public class Campaign : Entity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public ICollection<DynamicChallenge> Challenges { get; set; } = new List<DynamicChallenge>();
}
