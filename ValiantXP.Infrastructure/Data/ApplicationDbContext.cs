using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<DynamicChallenge> DynamicChallenges => Set<DynamicChallenge>();
    public DbSet<UserChallengeProgress> UserChallengeProgresses => Set<UserChallengeProgress>();
    public DbSet<Prize> Prizes => Set<Prize>();
    public DbSet<UserPrize> UserPrizes => Set<UserPrize>();
    public DbSet<Code> Codes => Set<Code>();
    public DbSet<FailedAttempt> FailedAttempts => Set<FailedAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
