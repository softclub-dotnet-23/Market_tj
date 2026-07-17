using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerProfileDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class FarmerProfileService(
    IFarmerProfileRepository farmerProfileRepository,
    IUserRepository userRepository,
    ILogger<FarmerProfileService> logger) : IFarmerProfileService
{
    public async Task<Result<IEnumerable<GetFarmerProfileDto>>> GetAllAsync()
    {
        try
        {
            var profiles = await farmerProfileRepository.GetAllAsync();
            return Result<IEnumerable<GetFarmerProfileDto>>.Ok(profiles.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка профилей фермеров");
            return Result<IEnumerable<GetFarmerProfileDto>>.Fail("Не удалось получить список профилей фермеров", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetFarmerProfileDto?>> GetByIdAsync(int id)
    {
        try
        {
            var profile = await farmerProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<GetFarmerProfileDto?>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            return Result<GetFarmerProfileDto?>.Ok(ToGetDto(profile));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении профиля фермера {Id}", id);
            return Result<GetFarmerProfileDto?>.Fail("Не удалось получить профиль фермера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateFarmerProfileDto dto)
    {
        try
        {
            var validation = FarmerProfileValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (dto.VerifiedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.VerifiedByAdminId.Value);
                if (admin is null || admin.Role != UserRole.Admin)
                    return Result<string>.Fail("VerifiedByAdminId должен ссылаться на существующего Admin", ErrorType.Validation);
            }

            // Раздел 9 ТЗ: User 1 — 1 FarmerProfile.
            var all = await farmerProfileRepository.GetAllAsync();
            if (all.Any(f => f.UserId == dto.UserId))
                return Result<string>.Fail("У этого пользователя уже есть профиль фермера", ErrorType.Conflict);

            var profile = new FarmerProfile
            {
                UserId = dto.UserId,
                FarmName = dto.FarmName,
                Region = dto.Region,
                District = dto.District,
                Village = dto.Village,
                Address = dto.Address,
                Description = dto.Description,
                VerificationStatus = dto.VerificationStatus,
                VerifiedAt = dto.VerifiedAt,
                VerifiedByAdminId = dto.VerifiedByAdminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await farmerProfileRepository.AddAsync(profile);
            return Result<string>.Ok("Профиль фермера создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании профиля фермера");
            return Result<string>.Fail("Не удалось создать профиль фермера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateFarmerProfileDto dto)
    {
        try
        {
            var validation = FarmerProfileValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var profile = await farmerProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (dto.VerifiedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.VerifiedByAdminId.Value);
                if (admin is null || admin.Role != UserRole.Admin)
                    return Result<string>.Fail("VerifiedByAdminId должен ссылаться на существующего Admin", ErrorType.Validation);
            }

            var all = await farmerProfileRepository.GetAllAsync();
            if (all.Any(f => f.Id != id && f.UserId == dto.UserId))
                return Result<string>.Fail("У этого пользователя уже есть профиль фермера", ErrorType.Conflict);

            profile.UserId = dto.UserId;
            profile.FarmName = dto.FarmName;
            profile.Region = dto.Region;
            profile.District = dto.District;
            profile.Village = dto.Village;
            profile.Address = dto.Address;
            profile.Description = dto.Description;
            profile.VerificationStatus = dto.VerificationStatus;
            profile.VerifiedAt = dto.VerifiedAt;
            profile.VerifiedByAdminId = dto.VerifiedByAdminId;
            profile.UpdatedAt = DateTime.UtcNow;

            await farmerProfileRepository.UpdateAsync(profile);
            return Result<string>.Ok("Профиль фермера обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении профиля фермера {Id}", id);
            return Result<string>.Fail("Не удалось обновить профиль фермера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var profile = await farmerProfileRepository.GetByIdAsync(id);
            if (profile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            await farmerProfileRepository.DeleteAsync(profile);
            return Result<string>.Ok("Профиль фермера удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении профиля фермера {Id}", id);
            return Result<string>.Fail("Не удалось удалить профиль фермера", ErrorType.InternalServerError);
        }
    }

    private static GetFarmerProfileDto ToGetDto(FarmerProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        FarmName = profile.FarmName,
        Region = profile.Region,
        District = profile.District,
        Village = profile.Village,
        Address = profile.Address,
        Description = profile.Description,
        VerificationStatus = profile.VerificationStatus,
        VerifiedAt = profile.VerifiedAt,
        VerifiedByAdminId = profile.VerifiedByAdminId,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
