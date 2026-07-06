using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class GuestSessionConfiguration : IEntityTypeConfiguration<GuestSession>
{
    public void Configure(EntityTypeBuilder<GuestSession> builder)
    {
        builder.ToTable("GuestSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExternalHint).HasMaxLength(500);
        builder.Property(x => x.ProgressJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.Token).IsUnique()
            .HasDatabaseName("UX_GuestSessions_Token");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_GuestSessions_ExpiresAt");

        // Nullable FK to User (only set after conversion)
        builder.HasOne(x => x.ConvertedToUser)
            .WithMany()
            .HasForeignKey(x => x.ConvertedToUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
