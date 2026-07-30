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
    IEmailVerificationService emailVerificationService,
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

            // Дополнено по явному запросу пользователя (раздел 23 ТЗ) — без
            // пройденного кода подтверждения регистрация невозможна, даже
            // если кто-то вызовет /api/auth/register напрямую, минуя форму.
            if (!await emailVerificationService.IsEmailVerifiedAsync(dto.Email))
                return Result<AuthResponseDto>.Fail("Email не подтверждён — сначала введите код из письма", ErrorType.Validation);

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

    public async Task<Result<string>> SendRegistrationVerificationCodeAsync(string email)
    {
        try
        {
            var existing = await userRepository.GetByEmailAsync(email);
            if (existing is not null)
                return Result<string>.Fail("Пользователь с таким Email уже существует", ErrorType.Conflict);

            return await emailVerificationService.SendCodeAsync(email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при отправке кода подтверждения для регистрации {Email}", email);
            return Result<string>.Fail("Не удалось отправить код подтверждения", ErrorType.InternalServerError);
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

    // Дополнено по явному запросу пользователя — раньше "Забыли пароль?" была
    // честной заглушкой без реализации. Переиспользует тот же код-на-email
    // механизм, что и подтверждение регистрации (IEmailVerificationService) —
    // отдельная сущность/таблица под сброс пароля не нужна, суть та же:
    // доказать владение email.
    public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequestDto dto)
    {
        try
        {
            var user = await userRepository.GetByEmailAsync(dto.Email);
            // Один и тот же ответ для существующего и несуществующего email —
            // не даём понять снаружи, зарегистрирован ли такой адрес (та же
            // причина, что и в LoginAsync).
            if (user is not null && user.IsActive)
                await emailVerificationService.SendCodeAsync(dto.Email);

            return Result<string>.Ok("Если аккаунт с таким email существует, на него отправлен код");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при запросе восстановления пароля для {Email}", dto.Email);
            return Result<string>.Fail("Не удалось отправить код восстановления", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return Result<string>.Fail("Пароль должен быть не короче 6 символов", ErrorType.Validation);

            var verifyResult = await emailVerificationService.VerifyCodeAsync(dto.Email, dto.Code);
            if (!verifyResult.IsSuccess)
                return Result<string>.Fail(verifyResult.Error!, verifyResult.ErrorType!.Value);

            var user = await userRepository.GetByEmailAsync(dto.Email);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user);

            return Result<string>.Ok("Пароль обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при сбросе пароля для {Email}", dto.Email);
            return Result<string>.Fail("Не удалось сбросить пароль", ErrorType.InternalServerError);
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
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.ToString()
        };
    }
}
