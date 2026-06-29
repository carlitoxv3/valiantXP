using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface ICodeRepository : IRepository<Code>
{
    Task<Code?> GetByCodeNumberAsync(string codeNumber, CancellationToken cancellationToken = default);
    Task BulkInsertAsync(IEnumerable<Code> codes, CancellationToken cancellationToken = default);
}
