using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Queries;

/// <summary>Returns a user's point balance and recent movements.</summary>
public record GetUserPointBalanceQuery(Guid UserId) : IRequest<UserPointBalanceDto>;

public record UserPointBalanceDto(
    Guid UserId,
    int TotalPoints,
    IReadOnlyList<PointMovementDto> RecentMovements);

public record PointMovementDto(
    Guid Id,
    int Points,
    string Source,
    string Description,
    DateTime CreatedAt,
    DateTime? ExpiresAt);

public class GetUserPointBalanceQueryHandler : IRequestHandler<GetUserPointBalanceQuery, UserPointBalanceDto>
{
    private readonly IUserPointMovementRepository _pointRepo;
    private readonly IApplicationDbContext _db;

    public GetUserPointBalanceQueryHandler(IUserPointMovementRepository pointRepo, IApplicationDbContext db)
    {
        _pointRepo = pointRepo;
        _db = db;
    }

    public async Task<UserPointBalanceDto> Handle(GetUserPointBalanceQuery request, CancellationToken cancellationToken)
    {
        var totalPoints = await _pointRepo.GetTotalPointsAsync(request.UserId, cancellationToken);

        var recentMovements = await _db.UserPointMovements
            .Where(m => m.UserId == request.UserId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .Select(m => new PointMovementDto(m.Id, m.Points, m.Source, m.Description, m.CreatedAt, m.ExpiresAt))
            .ToListAsync(cancellationToken);

        return new UserPointBalanceDto(request.UserId, totalPoints, recentMovements);
    }
}
