using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.HasKey(oc => oc.Id);

        builder.Property(oc => oc.Target)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(oc => oc.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(oc => oc.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(oc => new { oc.Target, oc.IsUsed, oc.ExpiresAt });
    }
}
