using MediatR;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Auth.Commands;

public record EnableMfaCommand(string Target, string Code) : IRequest<Result>;

public class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMfaService _mfaService;

    public EnableMfaCommandHandler(IUnitOfWork unitOfWork, IMfaService mfaService)
    {
        _unitOfWork = unitOfWork;
        _mfaService = mfaService;
    }

    public async Task<Result> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        bool isEmail = request.Target.Contains("@");
        var user = isEmail
            ? await _unitOfWork.Users.GetByEmailAsync(request.Target, cancellationToken)
            : await _unitOfWork.Users.GetByPhoneNumberAsync(request.Target, cancellationToken);

        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        if (string.IsNullOrEmpty(user.MfaSecret))
        {
            return Result.Failure("MFA setup is not initialized for this user.");
        }

        var isValid = _mfaService.VerifyMfaCode(user.MfaSecret, request.Code);
        if (!isValid)
        {
            return Result.Failure("Invalid verification code.");
        }

        user.IsMfaEnabled = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
