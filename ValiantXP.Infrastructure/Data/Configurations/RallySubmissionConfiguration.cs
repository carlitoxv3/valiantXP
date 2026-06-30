using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for RallySubmission.
/// Maps to table [RallySubmissions] with indexes on:
///   - SubmissionCode (unique)
///   - DynamicChallengeId + UserId (for gallery and limit queries)
///   - Status (for moderation queue filters)
/// </summary>
public sealed class RallySubmissionConfiguration : IEntityTypeConfiguration<RallySubmission>
{
    public void Configure(EntityTypeBuilder<RallySubmission> builder)
    {
        builder.ToTable("RallySubmissions");

        builder.HasKey(s => s.Id);

        // Unique submission code — like PromoHub's RallyMultimedia.Code
        builder.Property(s => s.SubmissionCode)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(s => s.SubmissionCode).IsUnique();

        builder.Property(s => s.RallyType).IsRequired();
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.IsWinner).IsRequired().HasDefaultValue(false);

        // Media content fields
        builder.Property(s => s.MediaUrl).HasMaxLength(2000);
        builder.Property(s => s.TextContent).HasMaxLength(2000);
        builder.Property(s => s.SubChallengeTag).HasMaxLength(500);
        builder.Property(s => s.RemoteIp).HasMaxLength(100);
        builder.Property(s => s.ModerationNotes).HasMaxLength(1000);

        // TicketDataJson stores the full ticket payload (no length cap — mirrors RallyMultimediaTicket.LineItems)
        builder.Property(s => s.TicketDataJson);

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.DynamicChallenge)
            .WithMany()
            .HasForeignKey(s => s.DynamicChallengeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Votes)
            .WithOne(v => v.RallySubmission)
            .HasForeignKey(v => v.RallySubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes
        builder.HasIndex(s => new { s.DynamicChallengeId, s.Status });
        builder.HasIndex(s => new { s.DynamicChallengeId, s.UserId });
    }
}
