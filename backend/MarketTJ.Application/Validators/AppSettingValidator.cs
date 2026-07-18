using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AppSettingDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class AppSettingValidator
{
    public static Result<string>? ValidateCreate(CreateAppSettingDto dto)
        => Validate(dto.Key, dto.Value);

    public static Result<string>? ValidateUpdate(UpdateAppSettingDto dto)
        => Validate(dto.Key, dto.Value);

    private static Result<string>? Validate(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Result<string>.Fail("Key обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(value))
            return Result<string>.Fail("Value обязателен", ErrorType.Validation);

        return null;
    }
}
