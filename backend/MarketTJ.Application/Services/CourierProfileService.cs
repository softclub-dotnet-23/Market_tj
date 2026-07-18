using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CourierProfileDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class CourierProfileService(
    ICourierProfileRepository courierProfileRepository,
    IUserRepository userRepository,
    ILogger<CourierProfileService> logger) : ICourierProfileService
{
    public async Task<Result<IEnumerable<GetCourierProfileDto>>> GetAllAsync()
    {
        try
        {
            var profiles = await courierProfileRepository.GetAllAsync();
            return Result<IEnumerable<GetCourierProfileDto>>.Ok(profiles.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка профилей курьеров");
            return Result<IEnumerable<GetCourierProfileDto>>.Fail("Не удалось получить список профилей курьеров", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetCourierProfileDto?>> GetByIdAsync(int id)
    {
        try
        {
            var profile = await courierProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<GetCourierProfileDto?>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            return Result<GetCourierProfileDto?>.Ok(ToGetDto(profile));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении профиля курьера {Id}", id);
            return Result<GetCourierProfileDto?>.Fail("Не удалось получить профиль курьера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateCourierProfileDto dto)
    {
        try
        {
            var validation = CourierProfileValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            // Раздел 9 ТЗ: User 1 — 1 CourierProfile.
            var all = await courierProfileRepository.GetAllAsync();
            if (all.Any(c => c.UserId == dto.UserId))
                return Result<string>.Fail("У этого пользователя уже есть профиль курьера", ErrorType.Conflict);

            var profile = new CourierProfile
            {
                UserId = dto.UserId,
                TransportType = dto.TransportType,
                VehicleNumber = dto.VehicleNumber,
                Region = dto.Region,
                District = dto.District,
                IsAvailable = dto.IsAvailable,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await courierProfileRepository.AddAsync(profile);
            return Result<string>.Ok("Профиль курьера создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании профиля курьера");
            return Result<string>.Fail("Не удалось создать профиль курьера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateCourierProfileDto dto)
    {
        try
        {
            var validation = CourierProfileValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var profile = await courierProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            var all = await courierProfileRepository.GetAllAsync();
            if (all.Any(c => c.Id != id && c.UserId == dto.UserId))
                return Result<string>.Fail("У этого пользователя уже есть профиль курьера", ErrorType.Conflict);

            profile.UserId = dto.UserId;
            profile.TransportType = dto.TransportType;
            profile.VehicleNumber = dto.VehicleNumber;
            profile.Region = dto.Region;
            profile.District = dto.District;
            profile.IsAvailable = dto.IsAvailable;
            profile.IsActive = dto.IsActive;
            profile.UpdatedAt = DateTime.UtcNow;

            await courierProfileRepository.UpdateAsync(profile);
            return Result<string>.Ok("Профиль курьера обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении профиля курьера {Id}", id);
            return Result<string>.Fail("Не удалось обновить профиль курьера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var profile = await courierProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            await courierProfileRepository.DeleteAsync(profile);
            return Result<string>.Ok("Профиль курьера удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении профиля курьера {Id}", id);
            return Result<string>.Fail("Не удалось удалить профиль курьера", ErrorType.InternalServerError);
        }
    }

    private static GetCourierProfileDto ToGetDto(CourierProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        TransportType = profile.TransportType,
        VehicleNumber = profile.VehicleNumber,
        Region = profile.Region,
        District = profile.District,
        IsAvailable = profile.IsAvailable,
        IsActive = profile.IsActive,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
