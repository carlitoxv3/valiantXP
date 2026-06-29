using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;

namespace ValiantXP.Infrastructure.Identity;

public class WhatsAppOtpSender : IWhatsAppOtpSender
{
    private readonly ILogger<WhatsAppOtpSender> _logger;

    public WhatsAppOtpSender(ILogger<WhatsAppOtpSender> logger)
    {
        _logger = logger;
    }

    public Task<Result> SendOtpAsync(string target, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SIMULATED WHATSAPP] Sending OTP code {Code} to {Target}", code, target);
        return Task.FromResult(Result.Success());
    }
}
