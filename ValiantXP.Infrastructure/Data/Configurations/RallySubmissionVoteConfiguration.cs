using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for RallySubmissionVote.
/// Unique index on (UserId, RallySubmissionId) enforces one-vote-per-user-per-submission at DB level.
/// Mirrors PromoHub's ProfileRallyMultimediaVote table.
/// </summary>
public sealed class RallySubmissionVoteConfiguration : IEntityTypeConfiguration<RallySubmissionVote>
{
    public void Configure(EntityTypeBuilder<RallySubmissionVote> builder)
    {
        builder.ToTable("RallySubmissionVotes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.RemoteIp).HasMaxLength(100);
        builder.Property(v => v.VotedAt).IsRequired();

        // Relationships
        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.RallySubmission)
            .WithMany(s => s.Votes)
            .HasForeignKey(v => v.RallySubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // DB-enforced uniqueness: one vote per user per submission
        builder.HasIndex(v => new { v.UserId, v.RallySubmissionId }).IsUnique();

        // For daily vote count query
        builder.HasIndex(v => new { v.UserId, v.VotedAt });
    }
}
