using System.Threading;
using System.Threading.Tasks;

namespace ValiantXP.Application.Identity;

public interface IOAuthProviderAdapter
{
    string ProviderName { get; }  // "google", "spotify", "twitch"
    string GetAuthorizationUrl(string redirectUri, string state);
    Task<ExternalIdentityClaims> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);
}
