using System.Threading.Tasks;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Entities;

namespace ValiantXP.Application.Common.Interfaces;

public interface ITokenService
{
    Task<TokenResponseDto> GenerateTokensAsync(User user, string ipAddress);
    Task<Result<TokenResponseDto>> RefreshTokenAsync(string token, string ipAddress);
}
