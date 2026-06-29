using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<DynamicChallenge> DynamicChallenges { get; }
    DbSet<UserChallengeProgress> UserChallengeProgresses { get; }
    DbSet<Prize> Prizes { get; }
    DbSet<UserPrize> UserPrizes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
