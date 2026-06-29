using System;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Enums;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Identity;

public class OtpService : IOtpService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailOtpSender _emailOtpSender;
    private readonly IWhatsAppOtpSender _whatsAppOtpSender;

    public OtpService(IUnitOfWork unitOfWork, IEmailOtpSender emailOtpSender, IWhatsAppOtpSender whatsAppOtpSender)
    {
        _unitOfWork = unitOfWork;
        _emailOtpSender = emailOtpSender;
        _whatsAppOtpSender = whatsAppOtpSender;
    }

    public async Task<Result> GenerateAndSendOtpAsync(string target, OtpChannel channel, CancellationToken cancellationToken = default)
    {
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        var otpCode = new OtpCode
        {
            Id = Guid.NewGuid(),
            Target = target,
            Code = code,
            Channel = channel,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            Attempts = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.OtpCodes.AddAsync(otpCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Result sendResult;
        if (channel == OtpChannel.Email)
        {
            sendResult = await _emailOtpSender.SendOtpAsync(target, code, cancellationToken);
        }
        else
        {
            sendResult = await _whatsAppOtpSender.SendOtpAsync(target, code, cancellationToken);
        }

        return sendResult;
    }

    public async Task<Result<User>> VerifyOtpAsync(string target, string code, CancellationToken cancellationToken = default)
    {
        var otpCode = await _unitOfWork.OtpCodes.GetLatestActiveOtpAsync(target, cancellationToken);

        if (otpCode == null)
        {
            return Result<User>.Failure("No active OTP request found.");
        }

        if (otpCode.IsUsed)
        {
            return Result<User>.Failure("OTP code has already been used.");
        }

        if (otpCode.ExpiresAt < DateTime.UtcNow)
        {
            return Result<User>.Failure("OTP code has expired.");
        }

        if (otpCode.Attempts >= 3)
        {
            otpCode.IsUsed = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<User>.Failure("Too many invalid verification attempts. Please request a new OTP.");
        }

        if (otpCode.Code != code)
        {
            otpCode.Attempts++;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<User>.Failure("Invalid OTP code.");
        }

        otpCode.IsUsed = true;

        bool isEmail = target.Contains("@");
        User? user;

        if (isEmail)
        {
            user = await _unitOfWork.Users.GetByEmailAsync(target, cancellationToken);
        }
        else
        {
            user = await _unitOfWork.Users.GetByPhoneNumberAsync(target, cancellationToken);
        }

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = isEmail ? target : string.Empty,
                UserName = target,
                PhoneNumber = isEmail ? null : target,
                IsActive = true,
                IsMfaEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<User>.Success(user);
    }
}
