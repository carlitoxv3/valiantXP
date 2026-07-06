using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class GiftCardRepository : GenericRepository<GiftCard>, IGiftCardRepository
{
    private readonly ApplicationDbContext _db;

    public GiftCardRepository(ApplicationDbContext context) : base(context)
    {
        _db = context;
    }

    /// <summary>
    /// Atomically assigns the next available gift card from the pool.
    /// Uses raw ADO.NET with UPDATE TOP(1) ... OUTPUT INSERTED.Id INTO @assigned
    /// to return the assigned ID, then loads the entity via EF FindAsync.
    /// This approach avoids EF's inability to map UPDATE OUTPUT columns directly.
    /// Race condition safe — mirrors PromoHub's dbo.SetGiftCard SP pattern.
    /// </summary>
    public async Task<GiftCard?> TryAssignFromPoolAsync(
        Guid providerId, Guid userId, Guid userPrizeId, CancellationToken ct = default)
    {
        const string sql = @"
            DECLARE @assigned TABLE (Id UNIQUEIDENTIFIER);
            UPDATE TOP(1) GiftCards
            SET AssignedToUserId = @userId,
                AssignedAt       = GETUTCDATE(),
                UserPrizeId      = @userPrizeId
            OUTPUT INSERTED.Id INTO @assigned
            WHERE ProviderId = @providerId AND AssignedToUserId IS NULL;
            SELECT Id FROM @assigned;";

        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        var pUserId = cmd.CreateParameter();
        pUserId.ParameterName = "@userId";
        pUserId.Value = userId;
        cmd.Parameters.Add(pUserId);

        var pUserPrizeId = cmd.CreateParameter();
        pUserPrizeId.ParameterName = "@userPrizeId";
        pUserPrizeId.Value = userPrizeId;
        cmd.Parameters.Add(pUserPrizeId);

        var pProviderId = cmd.CreateParameter();
        pProviderId.ParameterName = "@providerId";
        pProviderId.Value = providerId;
        cmd.Parameters.Add(pProviderId);

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result == DBNull.Value)
            return null;

        var assignedId = (Guid)result;
        return await _dbSet.FindAsync(new object[] { assignedId }, ct);
    }

    public async Task<int> GetAvailableCountAsync(Guid providerId, CancellationToken ct = default)
        => await _dbSet.CountAsync(x => x.ProviderId == providerId && x.AssignedToUserId == null, ct);

    public async Task BulkInsertAsync(IEnumerable<GiftCard> cards, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(cards, ct);

    public async Task<bool> CodeExistsAsync(Guid providerId, string code, CancellationToken ct = default)
        => await _dbSet.AnyAsync(x => x.ProviderId == providerId && x.Code == code, ct);
}
