using System.Text.RegularExpressions;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.PlatformSettingsDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static partial class PlatformSettingsValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegexPattern();

    public static Result<string>? ValidateUpdate(UpdatePlatformSettingsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SiteName))
            return Result<string>.Fail("Название сайта обязательно", ErrorType.Validation);

        if (dto.SiteName.Length > 100)
            return Result<string>.Fail("Название сайта не должно превышать 100 символов", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(dto.ContactEmail))
            return Result<string>.Fail("Контактный email обязателен", ErrorType.Validation);

        if (!EmailRegexPattern().IsMatch(dto.ContactEmail))
            return Result<string>.Fail("Контактный email имеет некорректный формат", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(dto.ContactPhone))
            return Result<string>.Fail("Контактный телефон обязателен", ErrorType.Validation);

        if (dto.CommissionPercent is < 0 or > 100)
            return Result<string>.Fail("Комиссия должна быть в диапазоне от 0 до 100%", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(dto.Currency))
            return Result<string>.Fail("Валюта обязательна", ErrorType.Validation);

        if (dto.Currency.Length > 10)
            return Result<string>.Fail("Код валюты не должен превышать 10 символов", ErrorType.Validation);

        if (dto.MinimumOrderAmount < 0)
            return Result<string>.Fail("Минимальная сумма заказа не может быть отрицательной", ErrorType.Validation);

        // Включённый maintenance-режим без сообщения оставляет пользователей
        // перед пустым экраном без объяснений — требуем текст, только когда
        // режим реально включён.
        if (dto.MaintenanceModeEnabled && string.IsNullOrWhiteSpace(dto.MaintenanceMessage))
            return Result<string>.Fail("При включённом режиме обслуживания сообщение для пользователей обязательно", ErrorType.Validation);

        if (dto.MaintenanceMessage is { Length: > 500 })
            return Result<string>.Fail("Сообщение о режиме обслуживания не должно превышать 500 символов", ErrorType.Validation);

        return null;
    }
}
