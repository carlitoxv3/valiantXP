using MediatR;
using System;

namespace ValiantXP.Domain.Events;

/// <summary>
/// Published when the InstantWin engine selects "no win" (null slot from AllowNoWin pool,
/// or no prizes available). Handlers can use this for analytics/logging.
/// </summary>
public sealed record NoPrizeEvent(
    Guid UserId,
    Guid ChallengeId,
    string Reason) : INotification;
