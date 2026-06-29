using System;
using System.Collections.Generic;

namespace ValiantXP.Domain.Entities;

public class DynamicContext
{
    public Guid DynamicId { get; set; }
    public Guid UserId { get; set; }
    public Dictionary<string, string> Inputs { get; set; } = new();
}
