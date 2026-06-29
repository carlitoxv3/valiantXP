using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

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
            .IsRequired()
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(p => p.DynamicChallenge)
            .WithMany(dc => dc.Prizes)
            .HasForeignKey(p => p.DynamicChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.UserPrizes)
            .WithOne(up => up.Prize)
            .HasForeignKey(up => up.PrizeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
