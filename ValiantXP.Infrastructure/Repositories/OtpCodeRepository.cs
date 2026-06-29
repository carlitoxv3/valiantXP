using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class OtpCodeRepository : GenericRepository<OtpCode>, IOtpCodeRepository
{
    public OtpCodeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<OtpCode?> GetLatestActiveOtpAsync(string target, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(oc => oc.Target == target && !oc.IsUsed && oc.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(oc => oc.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
