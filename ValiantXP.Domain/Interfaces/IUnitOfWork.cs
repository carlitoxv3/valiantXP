using System;
using System.Threading;
using System.Threading.Tasks;

namespace ValiantXP.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IOtpCodeRepository OtpCodes { get; }
    ICampaignRepository Campaigns { get; }
    IDynamicChallengeRepository DynamicChallenges { get; }
    IUserChallengeProgressRepository UserChallengeProgresses { get; }
    IPrizeRepository Prizes { get; }
    IUserPrizeRepository UserPrizes { get; }
    ICodeRepository Codes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
