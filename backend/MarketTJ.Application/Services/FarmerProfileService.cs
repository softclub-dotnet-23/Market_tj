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
    ICurrentUserService currentUser,
    ILogger<FarmerProfileService> logger) : IFarmerProfileService
{
    // GetAll/GetById сознательно ОСТАЮТСЯ публичными — это витрина фермера в
    // каталоге, доступная всем (в отличие от CustomerProfile/CourierProfile).
    // IDOR-guard нужен только на Create/Update/Delete (audit 2026-07-28, находка 2.2).
    public async Task<Result<IEnumerable<GetFarmerProfileDto>>> GetAllAsync()
    {
        try
        {
            var profiles = await farmerProfileRepository.GetAllAsync();
            // Публичная витрина (каталог/страница фермера) показывает аватарку
            // хозяйства — это аватар пользователя-владельца (User.AvatarUrl),
            // отдельного поля под фото у FarmerProfile нет. GetAllAsync() без
            // фильтра — тот же приём "грузим всё, сопоставляем в памяти", что и
            // везде в этом сервисе (проверка дублей UserId и т.п.).
            var users = await userRepository.GetAllAsync();
            var avatarByUserId = users.ToDictionary(u => u.Id, u => u.AvatarUrl);
            return Result<IEnumerable<GetFarmerProfileDto>>.Ok(
                profiles.Select(p => ToGetDto(p, avatarByUserId.GetValueOrDefault(p.UserId))));
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

            var user = await userRepository.GetByIdAsync(profile.UserId);
            return Result<GetFarmerProfileDto?>.Ok(ToGetDto(profile, user?.AvatarUrl));
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

            if (!currentUser.CanAccess(dto.UserId))
                return Result<string>.Fail("Нельзя создать профиль для другого пользователя", ErrorType.Forbidden);

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

            if (!currentUser.CanAccess(profile.UserId))
                return Result<string>.Fail("Нет доступа к этому профилю", ErrorType.Forbidden);

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

            if (!currentUser.CanAccess(profile.UserId))
                return Result<string>.Fail("Нет доступа к этому профилю", ErrorType.Forbidden);

            await farmerProfileRepository.DeleteAsync(profile);
            return Result<string>.Ok("Профиль фермера удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении профиля фермера {Id}", id);
            return Result<string>.Fail("Не удалось удалить профиль фермера", ErrorType.InternalServerError);
        }
    }

    private static GetFarmerProfileDto ToGetDto(FarmerProfile profile, string? avatarUrl = null) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        FarmName = profile.FarmName,
        Region = profile.Region,
        District = profile.District,
        Village = profile.Village,
        Address = profile.Address,
        Description = profile.Description,
        AvatarUrl = avatarUrl,
        VerificationStatus = profile.VerificationStatus,
        VerifiedAt = profile.VerifiedAt,
        VerifiedByAdminId = profile.VerifiedByAdminId,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
