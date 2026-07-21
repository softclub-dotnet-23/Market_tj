using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class AuthService(IUserRepository userRepository, ITokenService tokenService, ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        try
        {
            var user = await userRepository.GetByEmailAsync(dto.Email);
            // Один и тот же message и для "нет такого email", и для "неверный пароль",
            // и для неактивного пользователя — не даём понять снаружи, что именно не так.
            if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Result<LoginResponseDto>.Fail("Неверный email или пароль", ErrorType.Unauthorized);

            var token = tokenService.GenerateToken(user);
            return Result<LoginResponseDto>.Ok(new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString()
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при попытке входа для {Email}", dto.Email);
            return Result<LoginResponseDto>.Fail("Не удалось выполнить вход", ErrorType.InternalServerError);
        }
    }
}
