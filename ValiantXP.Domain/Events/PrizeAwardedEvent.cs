using MediatR;
using System;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Domain.Events;

/// <summary>
/// Published after a prize has been successfully awarded to a user.
/// Handlers can use this for logging, notifications, rank refresh, etc.
/// </summary>
public sealed record PrizeAwardedEvent(
    Guid UserId,
    Guid PrizeId,
    Guid UserPrizeId,
    PrizeType PrizeType,
    int PointsAwarded) : INotification;
