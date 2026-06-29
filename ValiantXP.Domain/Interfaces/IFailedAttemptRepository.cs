using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IFailedAttemptRepository : IRepository<FailedAttempt>
{
    /// <summary>
    /// Counts failed attempts for a specific user within a rolling time window.
    /// Equivalent to PromoHub's DetectBots SP parameter logic.
    /// </summary>
    Task<int> CountByUserAsync(Guid userId, Guid challengeId, int windowMinutes, CancellationToken ct = default);

    /// <summary>
    /// Counts failed attempts from a specific IP within a rolling time window.
    /// </summary>
    Task<int> CountByIpAsync(string remoteIp, Guid challengeId, int windowMinutes, CancellationToken ct = default);
}
