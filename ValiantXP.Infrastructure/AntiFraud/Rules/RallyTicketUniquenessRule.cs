using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Exceptions;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.AntiFraud.Rules;

/// <summary>
/// Prevents duplicate ticket numbers in Ticket and Consumption type rallies.
/// Mirrors PromoHub's ValidateTicketVendor SP that checks if the user already
/// submitted a given NroTicket for a Rally.
///
/// Rule only executes when:
///   - DynamicType = Rally
///   - RallyConfig.RequireUniqueTicketNumber = true (default)
///   - Input "ticketNumber" is present (Ticket/Consumption types)
///
/// Order 40 — runs after submission limit (30).
/// </summary>
public sealed class RallyTicketUniquenessRule : IAntiFraudRule
{
    private readonly IRallySubmissionRepository _submissionRepo;

    public DynamicType? ApplicableType => DynamicType.Rally;
    public int Order => 40;

    public RallyTicketUniquenessRule(IRallySubmissionRepository submissionRepo)
    {
        _submissionRepo = submissionRepo;
    }

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken cancellationToken)
    {
        // Only validate if ticket uniqueness is required AND a ticket number was submitted
        if (!context.Config.Rally.RequireUniqueTicketNumber) return;

        if (!context.Inputs.TryGetValue("ticketNumber", out var ticketNumber)
            || string.IsNullOrWhiteSpace(ticketNumber))
        {
            // No ticket number in inputs — this is a non-ticket Rally type (Photo, Story, etc.)
            return;
        }

        var exists = await _submissionRepo.TicketExistsAsync(
            context.ChallengeId, ticketNumber.Trim(), cancellationToken);

        if (exists)
        {
            throw AntiFraudException.RallyDuplicateTicket(ticketNumber.Trim());
        }
    }
}
