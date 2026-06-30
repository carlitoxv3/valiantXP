namespace ValiantXP.Domain.Enums;

/// <summary>
/// Determines how winners are selected from approved Rally submissions.
/// Configured per-campaign in DynamicChallenge.ConfigurationJson (RallyConfig.WinnerSelectionMode).
/// </summary>
public enum WinnerSelectionMode
{
    /// <summary>Winner(s) are manually chosen by an admin from the approved gallery.</summary>
    ByAdmin = 1,

    /// <summary>Winner(s) are the submission(s) with the highest vote count.</summary>
    ByVotes = 2,

    /// <summary>Winner(s) are randomly drawn from all approved submissions.</summary>
    ByLottery = 3,

    /// <summary>
    /// Winner(s) are determined by the total ticket purchase amount.
    /// Applies only to Ticket and Consumption rally types.
    /// </summary>
    ByTicketAmount = 4
}
