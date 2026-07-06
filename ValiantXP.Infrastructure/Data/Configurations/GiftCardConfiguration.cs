using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ToTable("GiftCards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(500);
        builder.Property(x => x.RedeemUrl).HasMaxLength(1000);
        builder.Property(x => x.Pin).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);

        // Unique code per provider
        builder.HasIndex(x => new { x.ProviderId, x.Code }).IsUnique()
            .HasDatabaseName("UX_GiftCards_Provider_Code");

        // Perf index for pool availability queries
        builder.HasIndex(x => new { x.ProviderId, x.AssignedToUserId })
            .HasDatabaseName("IX_GiftCards_Provider_Available");

        builder.HasOne(x => x.Provider).WithMany(x => x.GiftCards)
            .HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedToUser).WithMany()
            .HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);

        builder.HasOne(x => x.UserPrize).WithMany()
            .HasForeignKey(x => x.UserPrizeId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
    }
}
