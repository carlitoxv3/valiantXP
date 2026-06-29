using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class CodeRepository : GenericRepository<Code>, ICodeRepository
{
    public CodeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Code?> GetByCodeNumberAsync(string codeNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Code>()
            .FirstOrDefaultAsync(c => c.CodeNumber == codeNumber, cancellationToken);
    }

    public async Task BulkInsertAsync(IEnumerable<Code> codes, CancellationToken cancellationToken = default)
    {
        await _context.Set<Code>().AddRangeAsync(codes, cancellationToken);
    }
}
