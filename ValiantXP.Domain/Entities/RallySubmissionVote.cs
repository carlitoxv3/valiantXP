namespace ValiantXP.Domain.Entities;

/// <summary>
/// Records a community vote cast on a Rally submission.
/// One vote per user per submission (enforced by unique index on UserId + RallySubmissionId).
/// Mirrors PromoHub's ProfileRallyMultimediaVote entity.
/// </summary>
public class RallySubmissionVote
{
    public Guid Id { get; set; }

    public Guid RallySubmissionId { get; set; }
    public RallySubmission RallySubmission { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string RemoteIp { get; set; } = string.Empty;
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}
