using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.InstantWin.Strategies;

/// <summary>
/// Awards a Points prize:
///   1. Calculates final points (base, multiplier, ticket multiplier)
///   2. Creates UserPointMovement ledger entry
///   3. Atomically decrements stock via raw SQL (if stock-limited)
///   4. Creates UserPrize record
/// Maps to the Points branch of dbo.InstantWin_Save.
/// </summary>
public class PointsPrizeAwardStrategy : IPrizeAwardStrategy
{
    private readonly IApplicationDbContext _db;
    private readonly IUserPointMovementRepository _pointRepo;

    public PointsPrizeAwardStrategy(IApplicationDbContext db, IUserPointMovementRepository pointRepo)
    {
        _db = db;
        _pointRepo = pointRepo;
    }

    public bool CanHandle(PrizeType prizeType) => prizeType == PrizeType.Points;

    public async Task<UserPrize> AwardAsync(Prize prize, PrizeSelectionContext context, CancellationToken ct)
    {
        // 1. Calculate final points
        int finalPoints = prize.Quantity; // base points stored in Quantity

        if (prize.PointMultiplier > 0)
        {
            var userTotal = await _pointRepo.GetTotalPointsAsync(context.UserId, ct);
            finalPoints = prize.PointMultiplier * userTotal;
        }

        // Apply Rally ticket line items multiplier (mirrors OPENJSON logic in InstantWin_Save)
        if (context.TicketLineItemsJson != null)
        {
            var ticketMultiplier = ParseTicketQtySum(context.TicketLineItemsJson);
            if (ticketMultiplier > 0)
                finalPoints *= ticketMultiplier;
        }

        // 2. Set expiry
        DateTime? expiresAt = prize.PointsExpirationDays > 0
            ? context.Now.AddDays(prize.PointsExpirationDays)
            : null;

        // 3. Create movement ledger entry
        var movement = new UserPointMovement
        {
            Id = Guid.NewGuid(),
            UserId = context.UserId,
            Points = finalPoints,
            Source = "Challenge",
            Description = $"Prize: {prize.Name}",
            ChallengeId = context.ChallengeId,
            PrizeId = prize.Id,
            ExpiresAt = expiresAt,
            CreatedAt = context.Now
        };
        await _pointRepo.AddAsync(movement, ct);

        // 4. Atomic stock decrement — only when stock is limited
        if (prize.RemainingQuantity > 0)
        {
            await _db.TryDecrementPrizeStockAsync(prize.Id, ct);
        }

        // 5. Generate unique code
        var code = $"VXP-PTS-{Guid.NewGuid():N}"[..16].ToUpperInvariant();

        // 6. Create UserPrize record
        var userPrize = new UserPrize
        {
            Id = Guid.NewGuid(),
            UserId = context.UserId,
            PrizeId = prize.Id,
            PrizeType = PrizeType.Points,
            PointsAwarded = finalPoints,
            ExpiresAt = expiresAt,
            Code = code,
            AwardedAt = context.Now,
            SubmissionId = context.SubmissionId
        };

        await _db.UserPrizes.AddAsync(userPrize, ct);
        await _db.SaveChangesAsync(ct);

        return userPrize;
    }

    /// <summary>
    /// Parses JSON array of ticket line items and returns sum of quantities.
    /// Expected format: [{"qty": 2}, {"qty": 3}] or [{"quantity": 2}]
    /// </summary>
    private static int ParseTicketQtySum(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return 0;

            return doc.RootElement.EnumerateArray()
                .Sum(item =>
                {
                    if (item.TryGetProperty("qty", out var qty) && qty.TryGetInt32(out var q)) return q;
                    if (item.TryGetProperty("quantity", out var quantity) && quantity.TryGetInt32(out var q2)) return q2;
                    return 0;
                });
        }
        catch
        {
            return 0;
        }
    }
}
