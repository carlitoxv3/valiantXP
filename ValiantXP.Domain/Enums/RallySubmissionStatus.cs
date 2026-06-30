namespace ValiantXP.Domain.Enums;

/// <summary>
/// Lifecycle status of a Rally user submission (RallySubmission).
/// Transitions: PendingModeration → Approved | Rejected | Banned.
/// Only Approved submissions are eligible for voting and winner selection.
/// </summary>
public enum RallySubmissionStatus
{
    /// <summary>Submission received. Awaiting moderator review.</summary>
    PendingModeration = 1,

    /// <summary>Submission approved by moderator. Visible in gallery. Eligible for votes.</summary>
    Approved = 2,

    /// <summary>Submission rejected by moderator. Not visible in gallery. User notified.</summary>
    Rejected = 3,

    /// <summary>Submission banned (policy violation). User may be blocked from future submissions.</summary>
    Banned = 4
}
