using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValiantXP.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IOtpCodeRepository OtpCodes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
