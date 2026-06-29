using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class DynamicChallengeConfiguration : IEntityTypeConfiguration<DynamicChallenge>
{
    public void Configure(EntityTypeBuilder<DynamicChallenge> builder)
    {
        builder.HasKey(dc => dc.Id);

        builder.Property(dc => dc.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(dc => dc.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(dc => dc.ConfigurationJson)
            .IsRequired();

        builder.Property(dc => dc.IsActive)
            .IsRequired();

        // Relationships
        builder.HasOne(dc => dc.Campaign)
            .WithMany(c => c.Challenges)
            .HasForeignKey(dc => dc.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(dc => dc.Prizes)
            .WithOne(p => p.DynamicChallenge)
            .HasForeignKey(p => p.DynamicChallengeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(dc => dc.UserProgresses)
            .WithOne(ucp => ucp.DynamicChallenge)
            .HasForeignKey(ucp => ucp.DynamicChallengeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
