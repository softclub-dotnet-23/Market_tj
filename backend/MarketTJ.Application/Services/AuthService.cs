using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Проверка логина/пароля без выдачи токена — в проекте ещё нет системы
// аутентификации (JWT/Identity — "Этап 2", раздел 23 ТЗ). До появления
// JWT фронтенд хранит LoginResponseDto как признак "залогинен" (localStorage);
// когда появится Auth — этот сервис нужно будет заменить на выдачу токена.
public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return Result<LoginResponseDto>.Fail("Email обязателен", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(dto.Password))
                return Result<LoginResponseDto>.Fail("Пароль обязателен", ErrorType.Validation);

            // В IUserRepository нет GetByEmailAsync — только generic CRUD
            // (как и во всех остальных репозиториях проекта), поэтому поиск
            // по email через GetAllAsync() + LINQ.
            var users = await userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Email == dto.Email);

            if (user is null || !passwordHasher.Verify(dto.Password, user.PasswordHash))
                return Result<LoginResponseDto>.Fail("Неверный email или пароль", ErrorType.Unauthorized);

            if (!user.IsActive)
                return Result<LoginResponseDto>.Fail("Учётная запись отключена", ErrorType.Unauthorized);

            return Result<LoginResponseDto>.Ok(new LoginResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при входе пользователя {Email}", dto.Email);
            return Result<LoginResponseDto>.Fail("Не удалось выполнить вход", ErrorType.InternalServerError);
        }
    }
}
