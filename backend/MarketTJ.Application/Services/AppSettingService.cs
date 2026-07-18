using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AppSettingDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class AppSettingService(
    IAppSettingRepository appSettingRepository,
    IUserRepository userRepository,
    ILogger<AppSettingService> logger) : IAppSettingService
{
    public async Task<Result<IEnumerable<GetAppSettingDto>>> GetAllAsync()
    {
        try
        {
            var settings = await appSettingRepository.GetAllAsync();
            return Result<IEnumerable<GetAppSettingDto>>.Ok(settings.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка настроек");
            return Result<IEnumerable<GetAppSettingDto>>.Fail("Не удалось получить список настроек", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetAppSettingDto?>> GetByIdAsync(int id)
    {
        try
        {
            var setting = await appSettingRepository.GetByIdAsync(id);
            if (setting is null)
                return Result<GetAppSettingDto?>.Fail("Настройка не найдена", ErrorType.NotFound);

            return Result<GetAppSettingDto?>.Ok(ToGetDto(setting));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении настройки {Id}", id);
            return Result<GetAppSettingDto?>.Fail("Не удалось получить настройку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateAppSettingDto dto)
    {
        try
        {
            var validation = AppSettingValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var all = await appSettingRepository.GetAllAsync();
            if (all.Any(s => s.Key == dto.Key))
                return Result<string>.Fail("Настройка с таким Key уже существует", ErrorType.Conflict);

            if (dto.UpdatedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.UpdatedByAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("UpdatedByAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            var setting = new AppSetting
            {
                Key = dto.Key,
                Value = dto.Value,
                Description = dto.Description,
                UpdatedByAdminId = dto.UpdatedByAdminId,
                UpdatedAt = DateTime.UtcNow
            };

            await appSettingRepository.AddAsync(setting);
            return Result<string>.Ok("Настройка создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании настройки");
            return Result<string>.Fail("Не удалось создать настройку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateAppSettingDto dto)
    {
        try
        {
            var validation = AppSettingValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var setting = await appSettingRepository.GetByIdAsync(id);
            if (setting is null)
                return Result<string>.Fail("Настройка не найдена", ErrorType.NotFound);

            var all = await appSettingRepository.GetAllAsync();
            if (all.Any(s => s.Id != id && s.Key == dto.Key))
                return Result<string>.Fail("Настройка с таким Key уже существует", ErrorType.Conflict);

            if (dto.UpdatedByAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.UpdatedByAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("UpdatedByAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            setting.Key = dto.Key;
            setting.Value = dto.Value;
            setting.Description = dto.Description;
            setting.UpdatedByAdminId = dto.UpdatedByAdminId;
            setting.UpdatedAt = DateTime.UtcNow;

            await appSettingRepository.UpdateAsync(setting);
            return Result<string>.Ok("Настройка обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении настройки {Id}", id);
            return Result<string>.Fail("Не удалось обновить настройку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var setting = await appSettingRepository.GetByIdAsync(id);
            if (setting is null)
                return Result<string>.Fail("Настройка не найдена", ErrorType.NotFound);

            await appSettingRepository.DeleteAsync(setting);
            return Result<string>.Ok("Настройка удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении настройки {Id}", id);
            return Result<string>.Fail("Не удалось удалить настройку", ErrorType.InternalServerError);
        }
    }

    private static GetAppSettingDto ToGetDto(AppSetting setting) => new()
    {
        Id = setting.Id,
        Key = setting.Key,
        Value = setting.Value,
        Description = setting.Description,
        UpdatedAt = setting.UpdatedAt,
        UpdatedByAdminId = setting.UpdatedByAdminId
    };
}
