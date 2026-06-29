using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class UserChallengeProgressConfiguration : IEntityTypeConfiguration<UserChallengeProgress>
{
    public void Configure(EntityTypeBuilder<UserChallengeProgress> builder)
    {
        builder.HasKey(ucp => ucp.Id);

        builder.Property(ucp => ucp.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ucp => ucp.Attempts)
            .IsRequired();

        builder.Property(ucp => ucp.Score)
            .IsRequired();

        builder.Property(ucp => ucp.CompletedAt);

        // Relationships
        builder.HasOne(ucp => ucp.User)
            .WithMany()
            .HasForeignKey(ucp => ucp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ucp => ucp.DynamicChallenge)
            .WithMany(dc => dc.UserProgresses)
            .HasForeignKey(ucp => ucp.DynamicChallengeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index for one progress entry per user per challenge
        builder.HasIndex(ucp => new { ucp.UserId, ucp.DynamicChallengeId })
            .IsUnique();
    }
}
