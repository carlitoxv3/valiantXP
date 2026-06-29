using System;
using System.Collections.Generic;

namespace ValiantXP.Domain.Entities;

public class DynamicResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Payload { get; set; }
    public List<Guid> AwardedPrizeIds { get; set; } = new();
}
