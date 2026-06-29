using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ValiantXP.Application.AntiFraud;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.AntiFraud;
using ValiantXP.Domain.Interfaces;
using ValiantXP.Infrastructure.AntiFraud.Rules;
using ValiantXP.Infrastructure.Data;
using ValiantXP.Infrastructure.Dynamics;
using ValiantXP.Infrastructure.Identity;
using ValiantXP.Infrastructure.Repositories;

namespace ValiantXP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Repositories & UOW
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IDynamicChallengeRepository, DynamicChallengeRepository>();
        services.AddScoped<IUserChallengeProgressRepository, UserChallengeProgressRepository>();
        services.AddScoped<IPrizeRepository, PrizeRepository>();
        services.AddScoped<IUserPrizeRepository, UserPrizeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Identity
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IOtpService, OtpService>();

        // OTP Senders
        services.AddScoped<IEmailOtpSender, EmailOtpSender>();
        services.AddScoped<IWhatsAppOtpSender, WhatsAppOtpSender>();

        services.AddScoped<ICodeRepository, CodeRepository>();
        services.AddScoped<IFailedAttemptRepository, FailedAttemptRepository>();

        // Anti-fraud pipeline — rules registered in execution order
        // CampaignActiveWindowRule (Order 5)  — all dynamics
        services.AddScoped<IAntiFraudRule, CampaignActiveWindowRule>();
        // Codigo rules
        services.AddScoped<IAntiFraudRule, CodeExistsRule>();           // Order 10
        services.AddScoped<IAntiFraudRule, CodeNotUsedRule>();          // Order 20
        services.AddScoped<IAntiFraudRule, MaxRedemptionsPerUserRule>(); // Order 30
        services.AddScoped<IAntiFraudRule, MaxAttemptsPerIpRule>();     // Order 40
        services.AddScoped<IAntiFraudRule, FailedAttemptsBlockRule>();  // Order 50
        // Trivia rules
        services.AddScoped<IAntiFraudRule, MaxTriviaAttemptsRule>();    // Order 30
        // Encuesta rules
        services.AddScoped<IAntiFraudRule, SurveyOncePerUserRule>();    // Order 30
        // Pipeline orchestrator
        services.AddScoped<IAntiFraudPipeline, AntiFraudPipeline>();

        // Dynamics Engine
        services.AddScoped<IDynamicStrategy, TriviaStrategy>();
        services.AddScoped<IDynamicStrategy, SurveyStrategy>();
        services.AddScoped<IDynamicStrategy, CodeStrategy>();
        services.AddScoped<IDynamicService, DynamicService>();

        return services;
    }
}
