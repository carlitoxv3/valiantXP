using MediatR;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Auth.Commands;

public record VerifyMfaCommand(string Target, string Code, string IpAddress) : IRequest<Result<TokenResponseDto>>;

public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, Result<TokenResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMfaService _mfaService;
    private readonly ITokenService _tokenService;

    public VerifyMfaCommandHandler(IUnitOfWork unitOfWork, IMfaService mfaService, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _mfaService = mfaService;
        _tokenService = tokenService;
    }

    public async Task<Result<TokenResponseDto>> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        bool isEmail = request.Target.Contains("@");
        var user = isEmail
            ? await _unitOfWork.Users.GetByEmailAsync(request.Target, cancellationToken)
            : await _unitOfWork.Users.GetByPhoneNumberAsync(request.Target, cancellationToken);

        if (user == null)
        {
            return Result<TokenResponseDto>.Failure("User not found.");
        }

        if (!user.IsMfaEnabled || string.IsNullOrEmpty(user.MfaSecret))
        {
            return Result<TokenResponseDto>.Failure("MFA is not enabled for this user.");
        }

        var isValid = _mfaService.VerifyMfaCode(user.MfaSecret, request.Code);
        if (!isValid)
        {
            return Result<TokenResponseDto>.Failure("Invalid MFA verification code.");
        }

        var tokens = await _tokenService.GenerateTokensAsync(user, request.IpAddress);
        return Result<TokenResponseDto>.Success(tokens);
    }
}
