namespace ValiantXP.Domain.Enums;

/// <summary>
/// Specifies the media or content type of a Rally dynamic challenge.
/// Controls which submission fields are required and how validation is applied.
/// Mirrors PromoHub's RallyTypes enum.
/// </summary>
public enum RallyType
{
    /// <summary>User uploads a photo/image. MediaUrl is required.</summary>
    Photo = 1,

    /// <summary>User submits a proof-of-purchase ticket with line items. TicketDataJson is required.</summary>
    Ticket = 2,

    /// <summary>User submits a short narrative (text + optional image).</summary>
    Story = 3,

    /// <summary>User submits proof of a social media post (URL or screenshot).</summary>
    Social = 4,

    /// <summary>User submits a greeting card or mural dream (text + optional image).</summary>
    Card = 5,

    /// <summary>User submits proof of consumption/purchase receipt. TicketDataJson with NroTicket required.</summary>
    Consumption = 6
}
