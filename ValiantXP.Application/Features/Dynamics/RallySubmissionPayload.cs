namespace ValiantXP.Application.Features.Dynamics;

/// <summary>
/// Marker payload type returned by the Rally strategy.
/// When SubmitChallengeCommandHandler detects this type in DynamicResult.Payload,
/// it keeps the UserChallengeProgress in Pending status and skips ChallengeCompletedEvent.
/// The event will fire later via RallyWinnerSelectedEventHandler when winners are chosen.
/// </summary>
public sealed record RallySubmissionPayload(
    Guid SubmissionId,
    string SubmissionCode,
    string SubmissionStatus);
