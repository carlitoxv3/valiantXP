using MediatR;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;

namespace ValiantXP.Application.Features.Auth.Commands;

public record VerifyOtpCommand(string Target, string Code, string IpAddress) : IRequest<Result<VerifyOtpResponseDto>>;

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<VerifyOtpResponseDto>>
{
    private readonly IOtpService _otpService;
    private readonly ITokenService _tokenService;

    public VerifyOtpCommandHandler(IOtpService otpService, ITokenService tokenService)
    {
        _otpService = otpService;
        _tokenService = tokenService;
    }

    public async Task<Result<VerifyOtpResponseDto>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var verifyResult = await _otpService.VerifyOtpAsync(request.Target, request.Code, cancellationToken);
        if (!verifyResult.IsSuccess)
        {
            return Result<VerifyOtpResponseDto>.Failure(verifyResult.Error);
        }

        var user = verifyResult.Value!;

        if (user.IsMfaEnabled)
        {
            return Result<VerifyOtpResponseDto>.Success(new VerifyOtpResponseDto
            {
                IsMfaRequired = true,
                Tokens = null
            });
        }

        var tokens = await _tokenService.GenerateTokensAsync(user, request.IpAddress);
        return Result<VerifyOtpResponseDto>.Success(new VerifyOtpResponseDto
        {
            IsMfaRequired = false,
            Tokens = tokens
        });
    }
}
