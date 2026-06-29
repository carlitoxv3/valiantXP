using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

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

        // Relationships
        builder.HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Prize)
            .WithMany(p => p.UserPrizes)
            .HasForeignKey(up => up.PrizeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index for prize codes
        builder.HasIndex(up => up.Code)
            .IsUnique();
    }
}
