using System.Globalization;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.PlatformSettingsDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Общие настройки платформы (Admin → Настройки) — структурированная обёртка
// над generic key-value хранилищем AppSetting (см. PlatformSettingsKeys):
// набор из 11 фиксированных ключей читается/пишется одним запросом, а не
// через отдельный CRUD-экран на каждую настройку.
public class PlatformSettingsService(
    IAppSettingRepository appSettingRepository,
    ILogger<PlatformSettingsService> logger) : IPlatformSettingsService
{
    public async Task<Result<GetPlatformSettingsDto>> GetAsync()
    {
        try
        {
            var all = await appSettingRepository.GetAllAsync();
            var byKey = all.Where(s => s.Key.StartsWith("platform.", StringComparison.Ordinal))
                .ToDictionary(s => s.Key, s => s);

            var dto = new GetPlatformSettingsDto
            {
                SiteName = GetString(byKey, PlatformSettingsKeys.SiteName, "Market.tj"),
                LogoUrl = GetStringOrNull(byKey, PlatformSettingsKeys.LogoUrl),
                ContactEmail = GetString(byKey, PlatformSettingsKeys.ContactEmail, "info@market.tj"),
                ContactPhone = GetString(byKey, PlatformSettingsKeys.ContactPhone, "+992900000000"),
                CommissionPercent = GetDecimal(byKey, PlatformSettingsKeys.CommissionPercent, 10m),
                Currency = GetString(byKey, PlatformSettingsKeys.Currency, "TJS"),
                MinimumOrderAmount = GetDecimal(byKey, PlatformSettingsKeys.MinimumOrderAmount, 0m),
                MaintenanceModeEnabled = GetBool(byKey, PlatformSettingsKeys.MaintenanceModeEnabled, false),
                MaintenanceMessage = GetStringOrNull(byKey, PlatformSettingsKeys.MaintenanceMessage),
                EmailNotificationsEnabled = GetBool(byKey, PlatformSettingsKeys.EmailNotificationsEnabled, true),
                SmsNotificationsEnabled = GetBool(byKey, PlatformSettingsKeys.SmsNotificationsEnabled, false),
                UpdatedAt = byKey.Count > 0 ? byKey.Values.Max(s => s.UpdatedAt) : null,
                UpdatedByAdminId = byKey.Values.OrderByDescending(s => s.UpdatedAt).FirstOrDefault()?.UpdatedByAdminId
            };

            return Result<GetPlatformSettingsDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении настроек платформы");
            return Result<GetPlatformSettingsDto>.Fail("Не удалось получить настройки платформы", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(UpdatePlatformSettingsDto dto, int adminUserId)
    {
        try
        {
            var validation = PlatformSettingsValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var all = await appSettingRepository.GetAllAsync();
            var byKey = all.ToDictionary(s => s.Key, s => s);

            var values = new Dictionary<string, string>
            {
                [PlatformSettingsKeys.SiteName] = dto.SiteName,
                [PlatformSettingsKeys.LogoUrl] = dto.LogoUrl ?? "",
                [PlatformSettingsKeys.ContactEmail] = dto.ContactEmail,
                [PlatformSettingsKeys.ContactPhone] = dto.ContactPhone,
                [PlatformSettingsKeys.CommissionPercent] = dto.CommissionPercent.ToString(CultureInfo.InvariantCulture),
                [PlatformSettingsKeys.Currency] = dto.Currency,
                [PlatformSettingsKeys.MinimumOrderAmount] = dto.MinimumOrderAmount.ToString(CultureInfo.InvariantCulture),
                [PlatformSettingsKeys.MaintenanceModeEnabled] = dto.MaintenanceModeEnabled.ToString(),
                [PlatformSettingsKeys.MaintenanceMessage] = dto.MaintenanceMessage ?? "",
                [PlatformSettingsKeys.EmailNotificationsEnabled] = dto.EmailNotificationsEnabled.ToString(),
                [PlatformSettingsKeys.SmsNotificationsEnabled] = dto.SmsNotificationsEnabled.ToString()
            };

            var now = DateTime.UtcNow;
            foreach (var (key, category, description) in PlatformSettingsKeys.All)
            {
                if (byKey.TryGetValue(key, out var existing))
                {
                    existing.Value = values[key];
                    existing.Category = category;
                    existing.UpdatedByAdminId = adminUserId;
                    existing.UpdatedAt = now;
                    await appSettingRepository.UpdateAsync(existing);
                }
                else
                {
                    await appSettingRepository.AddAsync(new AppSetting
                    {
                        Key = key,
                        Value = values[key],
                        Category = category,
                        Description = description,
                        UpdatedByAdminId = adminUserId,
                        UpdatedAt = now
                    });
                }
            }

            return Result<string>.Ok("Настройки платформы обновлены");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении настроек платформы");
            return Result<string>.Fail("Не удалось обновить настройки платформы", ErrorType.InternalServerError);
        }
    }

    private static string GetString(Dictionary<string, AppSetting> byKey, string key, string fallback)
        => byKey.TryGetValue(key, out var s) && !string.IsNullOrEmpty(s.Value) ? s.Value : fallback;

    private static string? GetStringOrNull(Dictionary<string, AppSetting> byKey, string key)
        => byKey.TryGetValue(key, out var s) && !string.IsNullOrEmpty(s.Value) ? s.Value : null;

    private static decimal GetDecimal(Dictionary<string, AppSetting> byKey, string key, decimal fallback)
        => byKey.TryGetValue(key, out var s) && decimal.TryParse(s.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static bool GetBool(Dictionary<string, AppSetting> byKey, string key, bool fallback)
        => byKey.TryGetValue(key, out var s) && bool.TryParse(s.Value, out var value) ? value : fallback;
}
