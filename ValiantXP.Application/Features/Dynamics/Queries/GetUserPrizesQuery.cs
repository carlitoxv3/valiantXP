using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Application.Features.Dynamics.Queries;

/// <summary>Returns all prizes awarded to a specific user.</summary>
public record GetUserPrizesQuery(Guid UserId) : IRequest<IReadOnlyList<UserPrizeDto>>;

public record UserPrizeDto(
    Guid Id,
    Guid PrizeId,
    string PrizeName,
    PrizeType PrizeType,
    int PointsAwarded,
    string? GiftCardCode,
    string? GiftCardRedeemUrl,
    string Code,
    DateTime AwardedAt,
    DateTime? ExpiresAt,
    bool IsRedeemed);

public class GetUserPrizesQueryHandler : IRequestHandler<GetUserPrizesQuery, IReadOnlyList<UserPrizeDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUserPrizesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserPrizeDto>> Handle(GetUserPrizesQuery request, CancellationToken cancellationToken)
    {
        return await _db.UserPrizes
            .Where(up => up.UserId == request.UserId)
            .Include(up => up.Prize)
            .OrderByDescending(up => up.AwardedAt)
            .Select(up => new UserPrizeDto(
                up.Id,
                up.PrizeId,
                up.Prize.Name,
                up.PrizeType,
                up.PointsAwarded,
                up.GiftCardCode,
                _db.GiftCards
                    .Where(gc => gc.UserPrizeId == up.Id)
                    .Select(gc => gc.RedeemUrl)
                    .FirstOrDefault(),
                up.Code,
                up.AwardedAt,
                up.ExpiresAt,
                up.IsRedeemed))
            .ToListAsync(cancellationToken);
    }
}
