using System.Collections.Generic;

namespace ValiantXP.Application.Identity;

public class AnonParticipationConfig
{
    public bool Allowed { get; set; } = false;

    /// <summary>never | start | submit | prize_claim</summary>
    public string RequireAuthAt { get; set; } = "start";
    public int SessionTtlMinutes { get; set; } = 60;
    public List<string> AllowedProviders { get; set; } = new();

    /// <summary>keep_best | keep_authenticated | reject_anon</summary>
    public string OnConflict { get; set; } = "keep_authenticated";

    public static AnonParticipationConfig Default => new();
}
