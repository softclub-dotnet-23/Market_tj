using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<Result<string>> LogoutAsync(RefreshTokenRequestDto dto);
}
