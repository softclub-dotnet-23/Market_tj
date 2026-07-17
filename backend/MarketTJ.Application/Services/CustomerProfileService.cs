using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CustomerProfileDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class CustomerProfileService(
    ICustomerProfileRepository customerProfileRepository,
    IUserRepository userRepository,
    ILogger<CustomerProfileService> logger) : ICustomerProfileService
{
    public async Task<Result<IEnumerable<GetCustomerProfileDto>>> GetAllAsync()
    {
        try
        {
            var profiles = await customerProfileRepository.GetAllAsync();
            return Result<IEnumerable<GetCustomerProfileDto>>.Ok(profiles.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка профилей покупателей");
            return Result<IEnumerable<GetCustomerProfileDto>>.Fail("Не удалось получить список профилей покупателей", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetCustomerProfileDto?>> GetByIdAsync(int id)
    {
        try
        {
            var profile = await customerProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<GetCustomerProfileDto?>.Fail("Профиль покупателя не найден", ErrorType.NotFound);

            return Result<GetCustomerProfileDto?>.Ok(ToGetDto(profile));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении профиля покупателя {Id}", id);
            return Result<GetCustomerProfileDto?>.Fail("Не удалось получить профиль покупателя", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateCustomerProfileDto dto)
    {
        try
        {
            var validation = CustomerProfileValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            // Раздел 9 ТЗ: User 1 — 1 CustomerProfile.
            var all = await customerProfileRepository.GetAllAsync();
            if (all.Any(c => c.UserId == dto.UserId))
                return Result<string>.Fail("У этого пользователя уже есть профиль покупателя", ErrorType.Conflict);

            var profile = new CustomerProfile
            {
                UserId = dto.UserId,
                CustomerType = dto.CustomerType,
                DefaultAddress = dto.DefaultAddress,
                Region = dto.Region,
                District = dto.District,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await customerProfileRepository.AddAsync(profile);
            return Result<string>.Ok("Профиль покупателя создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании профиля покупателя");
            return Result<string>.Fail("Не удалось создать профиль покупателя", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateCustomerProfileDto dto)
    {
        try
        {
            var validation = CustomerProfileValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var profile = await customerProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<string>.Fail("Профиль покупателя не найден", ErrorType.NotFound);

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            var all = await customerProfileRepository.GetAllAsync();
            if (all.Any(c => c.Id != id && c.UserId == dto.UserId))
                return Result<string>.Fail("У этого пользователя уже есть профиль покупателя", ErrorType.Conflict);

            profile.UserId = dto.UserId;
            profile.CustomerType = dto.CustomerType;
            profile.DefaultAddress = dto.DefaultAddress;
            profile.Region = dto.Region;
            profile.District = dto.District;
            profile.UpdatedAt = DateTime.UtcNow;

            await customerProfileRepository.UpdateAsync(profile);
            return Result<string>.Ok("Профиль покупателя обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении профиля покупателя {Id}", id);
            return Result<string>.Fail("Не удалось обновить профиль покупателя", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var profile = await customerProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<string>.Fail("Профиль покупателя не найден", ErrorType.NotFound);

            await customerProfileRepository.DeleteAsync(profile);
            return Result<string>.Ok("Профиль покупателя удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении профиля покупателя {Id}", id);
            return Result<string>.Fail("Не удалось удалить профиль покупателя", ErrorType.InternalServerError);
        }
    }

    private static GetCustomerProfileDto ToGetDto(CustomerProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        CustomerType = profile.CustomerType,
        DefaultAddress = profile.DefaultAddress,
        Region = profile.Region,
        District = profile.District,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
