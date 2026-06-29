using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;

namespace ValiantXP.Application.Common.Interfaces;

public interface IOtpSender
{
    Task<Result> SendOtpAsync(string target, string code, CancellationToken cancellationToken = default);
}
