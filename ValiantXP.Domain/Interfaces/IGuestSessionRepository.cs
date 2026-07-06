using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IGuestSessionRepository
{
    Task<GuestSession?> FindByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(GuestSession session, CancellationToken ct = default);
    Task UpdateAsync(GuestSession session, CancellationToken ct = default);
    Task ExpireOldSessionsAsync(CancellationToken ct = default);
}
