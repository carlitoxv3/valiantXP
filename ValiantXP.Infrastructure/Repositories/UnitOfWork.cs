using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IUserRepository? _users;
    private IRefreshTokenRepository? _refreshTokens;
    private IOtpCodeRepository? _otpCodes;
    private ICampaignRepository? _campaigns;
    private IDynamicChallengeRepository? _dynamicChallenges;
    private IUserChallengeProgressRepository? _userChallengeProgresses;
    private IPrizeRepository? _prizes;
    private IUserPrizeRepository? _userPrizes;
    private IUserPointMovementRepository? _userPointMovements;
    private ICodeRepository? _codes;
    private IFailedAttemptRepository? _failedAttempts;
    private IUserIdentityRepository? _userIdentities;
    private IGuestSessionRepository? _guestSessions;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);
    public IOtpCodeRepository OtpCodes => _otpCodes ??= new OtpCodeRepository(_context);
    public ICampaignRepository Campaigns => _campaigns ??= new CampaignRepository(_context);
    public IDynamicChallengeRepository DynamicChallenges => _dynamicChallenges ??= new DynamicChallengeRepository(_context);
    public IUserChallengeProgressRepository UserChallengeProgresses => _userChallengeProgresses ??= new UserChallengeProgressRepository(_context);
    public IPrizeRepository Prizes => _prizes ??= new PrizeRepository(_context);
    public IUserPrizeRepository UserPrizes => _userPrizes ??= new UserPrizeRepository(_context);
    public IUserPointMovementRepository UserPointMovements => _userPointMovements ??= new UserPointMovementRepository(_context);
    public ICodeRepository Codes => _codes ??= new CodeRepository(_context);
    public IFailedAttemptRepository FailedAttempts => _failedAttempts ??= new FailedAttemptRepository(_context);
    public IUserIdentityRepository UserIdentities => _userIdentities ??= new UserIdentityRepository(_context);
    public IGuestSessionRepository GuestSessions => _guestSessions ??= new GuestSessionRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    private bool _disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
