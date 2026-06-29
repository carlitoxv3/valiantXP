using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Auth.Commands;

public record RequestOtpCommand(string Target, OtpChannel Channel) : IRequest<Result>;

public class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailOtpSender _emailOtpSender;
    private readonly IWhatsAppOtpSender _whatsAppOtpSender;

    public RequestOtpCommandHandler(IUnitOfWork unitOfWork, IEmailOtpSender emailOtpSender, IWhatsAppOtpSender whatsAppOtpSender)
    {
        _unitOfWork = unitOfWork;
        _emailOtpSender = emailOtpSender;
        _whatsAppOtpSender = whatsAppOtpSender;
    }

    public async Task<Result> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Target))
        {
            return Result.Failure("Target (email or phone) is required.");
        }

        // Generate 6-digit OTP code
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            Target = request.Target,
            Code = code,
            Channel = request.Channel,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            Attempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.OtpCodes.AddAsync(otpCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send OTP
        Result sendResult;
        if (request.Channel == OtpChannel.Email)
        {
            sendResult = await _emailOtpSender.SendOtpAsync(request.Target, code, cancellationToken);
        }
        else
        {
            sendResult = await _whatsAppOtpSender.SendOtpAsync(request.Target, code, cancellationToken);
        }

        if (!sendResult.IsSuccess)
        {
            return Result.Failure($"Failed to send OTP: {sendResult.Error}");
        }

        return Result.Success();
    }
}
