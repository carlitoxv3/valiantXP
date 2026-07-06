using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class PrizeConfiguration : IEntityTypeConfiguration<Prize>
{
    public void Configure(EntityTypeBuilder<Prize> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.Quantity)
            .IsRequired();

        builder.Property(p => p.RemainingQuantity)
            .IsRequired();

        builder.Property(p => p.Type)
            .IsRequired(false) // legacy field — nullable for new records
            .HasMaxLength(100);

        // --- InstantWin extension fields ---
        builder.Property(p => p.PrizeType)
            .IsRequired()
            .HasConversion<int>(); // stored as int for DB compat

        builder.Property(p => p.AllowNoWin)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.WindowHours)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.MaxGlobalInWindow)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.MaxPerUserInWindow)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.PointMultiplier)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.PointsExpirationDays)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.ExternalReference)
            .HasMaxLength(256);

        builder.Property(p => p.Description)
            .HasMaxLength(1024);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(512);

        // Relationships
        builder.HasOne(p => p.DynamicChallenge)
            .WithMany(dc => dc.Prizes)
            .HasForeignKey(p => p.DynamicChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Fix: UserPrize owns the FK-side, so configure here without OnDelete to avoid conflict
        // (UserPrizeConfiguration configures the Restrict behavior on the FK side)
        builder.HasMany(p => p.UserPrizes)
            .WithOne(up => up.Prize)
            .HasForeignKey(up => up.PrizeId)
            .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to fix the conflict

        // GiftCard pool delivery channel (Sprint 10) — optional FK to GiftCardProvider.
        // WithMany(x => x.Prizes) matches the ICollection<Prize> on GiftCardProvider,
        // preventing EF from creating a shadow 'GiftCardProviderId1' property.
        builder.HasOne(x => x.GiftCardProvider)
            .WithMany(x => x.Prizes)
            .HasForeignKey(x => x.GiftCardProviderId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
