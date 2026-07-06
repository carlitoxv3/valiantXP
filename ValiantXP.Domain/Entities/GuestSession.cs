using System;
using ValiantXP.Domain.Common;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Entities;

public class GuestSession : Entity
{
    public string Token { get; set; } = string.Empty;
    public IdentityProvider ChannelHint { get; set; } = IdentityProvider.EmailOtp;
    public string? ExternalHint { get; set; }
    public int TtlMinutes { get; set; } = 60;
    public DateTime ExpiresAt { get; set; }

    public Guid? ConvertedToUserId { get; set; }
    public User? ConvertedToUser { get; set; }
    public DateTime? ConvertedAt { get; set; }

    public Guid? ActiveChallengeId { get; set; }
    public string? ProgressJson { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsConverted => ConvertedToUserId.HasValue;
}
