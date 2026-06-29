using MediatR;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.Common.Interfaces;
using ValiantXP.Application.DTOs;

namespace ValiantXP.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken, string IpAddress) : IRequest<Result<TokenResponseDto>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenResponseDto>>
{
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<Result<TokenResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await _tokenService.RefreshTokenAsync(request.RefreshToken, request.IpAddress);
    }
}
