using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("UserIdentities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(500);
        builder.Property(x => x.EmailClaim).HasMaxLength(320);
        builder.Property(x => x.ClaimsJson).HasColumnType("nvarchar(max)");

        // THE critical constraint: same provider+externalId can only belong to one active user
        builder.HasIndex(x => new { x.Provider, x.ExternalId })
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UX_UserIdentities_Provider_ExternalId_Active");

        // For email-based auto-merge lookup
        builder.HasIndex(x => new { x.EmailClaim, x.IsEmailVerified })
            .HasFilter("[IsEmailVerified] = 1 AND [IsActive] = 1")
            .HasDatabaseName("IX_UserIdentities_EmailClaim_Verified");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_UserIdentities_UserId");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
