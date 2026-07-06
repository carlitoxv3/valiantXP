using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class UserPrizeConfiguration : IEntityTypeConfiguration<UserPrize>
{
    public void Configure(EntityTypeBuilder<UserPrize> builder)
    {
        builder.HasKey(up => up.Id);

        builder.Property(up => up.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(up => up.AwardedAt)
            .IsRequired();

        // --- InstantWin extension fields ---
        builder.Property(up => up.PrizeType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(up => up.PointsAwarded)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(up => up.GiftCardCode)
            .HasMaxLength(256);

        builder.Property(up => up.ExpiresAt);

        builder.Property(up => up.SubmissionId);

        builder.Property(up => up.IsRedeemed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(up => up.RedeemedAt);

        // Relationships
        builder.HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Prize)
            .WithMany(p => p.UserPrizes)
            .HasForeignKey(up => up.PrizeId)
            .OnDelete(DeleteBehavior.Restrict); // Restrict to avoid cascade conflict with DynamicChallenge

        // Unique index for prize codes (prevents duplicate codes in DB)
        builder.HasIndex(up => up.Code)
            .IsUnique();

        // Composite index for efficient window queries (GetAwardCountInWindowAsync, GetUserAwardCountInWindowAsync)
        builder.HasIndex(up => new { up.UserId, up.PrizeId });
        builder.HasIndex(up => new { up.PrizeId, up.AwardedAt });
    }
}
