using MediatR;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Auth.Commands;

public record SetupMfaCommand(string Target) : IRequest<Result<MfaSetupDto>>;

public class SetupMfaCommandHandler : IRequestHandler<SetupMfaCommand, Result<MfaSetupDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMfaService _mfaService;

    public SetupMfaCommandHandler(IUnitOfWork unitOfWork, IMfaService mfaService)
    {
        _unitOfWork = unitOfWork;
        _mfaService = mfaService;
    }

    public async Task<Result<MfaSetupDto>> Handle(SetupMfaCommand request, CancellationToken cancellationToken)
    {
        bool isEmail = request.Target.Contains("@");
        var user = isEmail
            ? await _unitOfWork.Users.GetByEmailAsync(request.Target, cancellationToken)
            : await _unitOfWork.Users.GetByPhoneNumberAsync(request.Target, cancellationToken);

        if (user == null)
        {
            return Result<MfaSetupDto>.Failure("User not found.");
        }

        var setup = _mfaService.GenerateMfaSetup(request.Target);

        user.MfaSecret = setup.Secret;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<MfaSetupDto>.Success(setup);
    }
}
