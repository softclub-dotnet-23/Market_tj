using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.UserDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class UserService(IUserRepository userRepository, ILogger<UserService> logger) : IUserService
{
    public async Task<Result<IEnumerable<GetUserDto>>> GetAllAsync()
    {
        try
        {
            var users = await userRepository.GetAllAsync();
            return Result<IEnumerable<GetUserDto>>.Ok(users.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка пользователей");
            return Result<IEnumerable<GetUserDto>>.Fail("Не удалось получить список пользователей", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetUserDto?>> GetByIdAsync(int id)
    {
        try
        {
            var user = await userRepository.GetByIdAsync(id);
            if (user is null)
                return Result<GetUserDto?>.Fail("Пользователь не найден", ErrorType.NotFound);

            return Result<GetUserDto?>.Ok(ToGetDto(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении пользователя {Id}", id);
            return Result<GetUserDto?>.Fail("Не удалось получить пользователя", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateUserDto dto)
    {
        try
        {
            var validation = UserValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            // Раздел 8.1 ТЗ: Email и PhoneNumber уникальны. В IUserRepository
            // нет ExistsByEmailAsync — только generic CRUD, поэтому проверка
            // через GetAllAsync() (не оптимально для больших объёмов, но
            // репозиторий не расширяю сверх того, что реально есть).
            var all = await userRepository.GetAllAsync();
            if (all.Any(u => u.Email == dto.Email))
                return Result<string>.Fail("Пользователь с таким Email уже существует", ErrorType.Conflict);

            if (all.Any(u => u.PhoneNumber == dto.PhoneNumber))
                return Result<string>.Fail("Пользователь с таким номером телефона уже существует", ErrorType.Conflict);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = dto.PasswordHash,
                Role = dto.Role,
                IsActive = dto.IsActive,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await userRepository.AddAsync(user);
            return Result<string>.Ok("Пользователь создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании пользователя");
            return Result<string>.Fail("Не удалось создать пользователя", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateUserDto dto)
    {
        try
        {
            var validation = UserValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var user = await userRepository.GetByIdAsync(id);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            var all = await userRepository.GetAllAsync();
            if (all.Any(u => u.Id != id && u.Email == dto.Email))
                return Result<string>.Fail("Пользователь с таким Email уже существует", ErrorType.Conflict);

            if (all.Any(u => u.Id != id && u.PhoneNumber == dto.PhoneNumber))
                return Result<string>.Fail("Пользователь с таким номером телефона уже существует", ErrorType.Conflict);

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.PasswordHash = dto.PasswordHash;
            user.Role = dto.Role;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await userRepository.UpdateAsync(user);
            return Result<string>.Ok("Пользователь обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении пользователя {Id}", id);
            return Result<string>.Fail("Не удалось обновить пользователя", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var user = await userRepository.GetByIdAsync(id);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            // У User есть IsDeleted/DeletedAt (раздел 18 ТЗ — soft delete для
            // важных данных) — удаление помечает запись, а не стирает физически.
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await userRepository.UpdateAsync(user);
            return Result<string>.Ok("Пользователь удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении пользователя {Id}", id);
            return Result<string>.Fail("Не удалось удалить пользователя", ErrorType.InternalServerError);
        }
    }

    private static GetUserDto ToGetDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
