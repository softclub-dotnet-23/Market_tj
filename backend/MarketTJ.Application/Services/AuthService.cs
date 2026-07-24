using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto)
    {
        try
        {
            var validation = AuthValidator.ValidateRegister(dto);
            if (validation is not null)
                return Result<AuthResponseDto>.Fail(validation.Error!, validation.ErrorType!.Value);

            var existing = await userRepository.GetByEmailAsync(dto.Email);
            if (existing is not null)
                return Result<AuthResponseDto>.Fail("Пользователь с таким Email уже существует", ErrorType.Conflict);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await userRepository.AddAsync(user);

            // Раздел 23 ТЗ, Этап 3: заполнение FarmerProfile/CustomerProfile —
            // отдельный шаг после регистрации (POST /api/farmer-profiles,
            // /api/customer-profiles уже с выданным здесь токеном), не часть
            // этого метода.
            return Result<AuthResponseDto>.Ok(await IssueTokensAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при регистрации пользователя {Email}", dto.Email);
            return Result<AuthResponseDto>.Fail("Не удалось зарегистрироваться", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        try
        {
            var user = await userRepository.GetByEmailAsync(dto.Email);
            // Один и тот же message и для "нет такого email", и для "неверный пароль",
            // и для неактивного пользователя — не даём понять снаружи, что именно не так.
            if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Result<AuthResponseDto>.Fail("Неверный email или пароль", ErrorType.Unauthorized);

            return Result<AuthResponseDto>.Ok(await IssueTokensAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при попытке входа для {Email}", dto.Email);
            return Result<AuthResponseDto>.Fail("Не удалось выполнить вход", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        try
        {
            var existing = await refreshTokenRepository.GetByTokenAsync(dto.RefreshToken);
            if (existing is null || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow)
                return Result<AuthResponseDto>.Fail("Недействительный refresh token", ErrorType.Unauthorized);

            var user = await userRepository.GetByIdAsync(existing.UserId);
            if (user is null || !user.IsActive)
                return Result<AuthResponseDto>.Fail("Недействительный refresh token", ErrorType.Unauthorized);

            // Ротация: предъявленный refresh token отзывается сразу, выдаётся
            // новая пара. Если его попробуют предъявить повторно (кража/replay) —
            // ветка выше (IsRevoked) отклонит запрос.
            existing.IsRevoked = true;
            await refreshTokenRepository.UpdateAsync(existing);

            return Result<AuthResponseDto>.Ok(await IssueTokensAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении токена");
            return Result<AuthResponseDto>.Fail("Не удалось обновить токен", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> LogoutAsync(RefreshTokenRequestDto dto)
    {
        try
        {
            var existing = await refreshTokenRepository.GetByTokenAsync(dto.RefreshToken);
            if (existing is null)
                return Result<string>.Ok("Выход выполнен");

            existing.IsRevoked = true;
            await refreshTokenRepository.UpdateAsync(existing);

            return Result<string>.Ok("Выход выполнен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при выходе из системы");
            return Result<string>.Fail("Не удалось выполнить выход", ErrorType.InternalServerError);
        }
    }

    private async Task<AuthResponseDto> IssueTokensAsync(User user)
    {
        var accessToken = tokenService.GenerateToken(user);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        await refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(tokenService.RefreshTokenExpiryDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        return new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(tokenService.AccessTokenExpiryMinutes),
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString()
        };
    }
}
