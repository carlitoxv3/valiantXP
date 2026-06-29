using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Domain.Interfaces;

public interface IOtpCodeRepository : IRepository<OtpCode>
{
    Task<OtpCode?> GetLatestActiveOtpAsync(string target, CancellationToken cancellationToken = default);
}
