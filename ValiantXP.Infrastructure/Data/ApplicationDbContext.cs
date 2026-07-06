using Microsoft.EntityFrameworkCore;
using System;
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
    public DbSet<UserPointMovement> UserPointMovements => Set<UserPointMovement>();
    public DbSet<Code> Codes => Set<Code>();
    public DbSet<FailedAttempt> FailedAttempts => Set<FailedAttempt>();
    public DbSet<RallySubmission> RallySubmissions => Set<RallySubmission>();
    public DbSet<RallySubmissionVote> RallySubmissionVotes => Set<RallySubmissionVote>();

    /// <inheritdoc/>
    public async Task<bool> TryDecrementPrizeStockAsync(Guid prizeId, CancellationToken ct = default)
    {
        // Raw SQL atomic decrement — equivalent to PromoHub's stock decrement in InstantWin_Save
        // Returns 1 if updated, 0 if stock was already 0 (race condition protection)
        int rows = await Database.ExecuteSqlRawAsync(
            "UPDATE Prizes SET RemainingQuantity = RemainingQuantity - 1 WHERE Id = {0} AND RemainingQuantity > 0",
            prizeId);
        return rows > 0;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
