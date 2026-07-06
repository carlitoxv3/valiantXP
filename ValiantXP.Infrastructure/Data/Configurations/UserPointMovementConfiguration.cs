using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class UserPointMovementConfiguration : IEntityTypeConfiguration<UserPointMovement>
{
    public void Configure(EntityTypeBuilder<UserPointMovement> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Points)
            .IsRequired();

        builder.Property(m => m.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(m => m.ChallengeId);
        builder.Property(m => m.PrizeId);
        builder.Property(m => m.ExpiresAt);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        // Relationship: User → UserPointMovements
        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for GetTotalPointsAsync (sum non-expired by user)
        builder.HasIndex(m => new { m.UserId, m.ExpiresAt });
        // Index for audit/reporting queries
        builder.HasIndex(m => m.CreatedAt);
    }
}
