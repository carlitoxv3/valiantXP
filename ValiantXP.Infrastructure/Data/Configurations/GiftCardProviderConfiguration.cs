using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class GiftCardProviderConfiguration : IEntityTypeConfiguration<GiftCardProvider>
{
    public void Configure(EntityTypeBuilder<GiftCardProvider> builder)
    {
        builder.ToTable("GiftCardProviders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.InstructiveUrl).HasMaxLength(500);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.HasIndex(x => x.Name).HasDatabaseName("IX_GiftCardProviders_Name");
        builder.HasOne(x => x.Campaign)
            .WithMany()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
