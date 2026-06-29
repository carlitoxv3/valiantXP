using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public sealed class FailedAttemptConfiguration : IEntityTypeConfiguration<FailedAttempt>
{
    public void Configure(EntityTypeBuilder<FailedAttempt> builder)
    {
        builder.ToTable("FailedAttempts");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.RuleCode).IsRequired().HasMaxLength(64);
        builder.Property(f => f.Reason).HasMaxLength(512);
        builder.Property(f => f.SubmittedValue).HasMaxLength(256);
        builder.Property(f => f.RemoteIp).HasMaxLength(64);
        builder.Property(f => f.AttemptedAt).IsRequired();

        // Indexes for the rolling-window COUNT queries used by anti-fraud rules
        builder.HasIndex(f => new { f.UserId, f.ChallengeId, f.AttemptedAt })
               .HasDatabaseName("IX_FailedAttempts_User_Challenge_Date");

        builder.HasIndex(f => new { f.RemoteIp, f.ChallengeId, f.AttemptedAt })
               .HasDatabaseName("IX_FailedAttempts_Ip_Challenge_Date");
    }
}
