using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IUserPrizeRepository : IRepository<UserPrize>
{
    Task<IEnumerable<UserPrize>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts how many times a prize was awarded globally within the time window.
    /// Used by GlobalWindowFilter to enforce MaxGlobalInWindow.
    /// </summary>
    Task<int> GetAwardCountInWindowAsync(Guid prizeId, DateTime windowStart, CancellationToken ct = default);

    /// <summary>
    /// Counts how many times a specific user won a prize within the time window.
    /// Used by PerUserWindowFilter to enforce MaxPerUserInWindow.
    /// </summary>
    Task<int> GetUserAwardCountInWindowAsync(Guid userId, Guid prizeId, DateTime windowStart, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the user has already won this prize at least once (for Product uniqueness).
    /// Used by UserAlreadyWonFilter.
    /// </summary>
    Task<bool> UserAlreadyWonAsync(Guid userId, Guid prizeId, CancellationToken ct = default);
}
