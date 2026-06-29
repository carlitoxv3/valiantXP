using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;

namespace ValiantXP.Application.Common.Interfaces;

public interface IOtpService
{
    Task<Result> GenerateAndSendOtpAsync(string target, OtpChannel channel, CancellationToken cancellationToken = default);
    Task<Result<User>> VerifyOtpAsync(string target, string code, CancellationToken cancellationToken = default);
}
