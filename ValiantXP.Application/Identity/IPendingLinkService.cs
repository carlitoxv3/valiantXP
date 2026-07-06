using System.Threading;
using System.Threading.Tasks;

namespace ValiantXP.Application.Identity;

public interface IPendingLinkService
{
    Task<string> CreateAsync(ExternalIdentityClaims claims, CancellationToken ct = default);
    Task<ExternalIdentityClaims?> ValidateAndConsumeAsync(string token, CancellationToken ct = default);
}
