using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IUserChallengeProgressRepository : IRepository<UserChallengeProgress>
{
    Task<UserChallengeProgress?> GetByUserAndChallengeAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken = default);
}
