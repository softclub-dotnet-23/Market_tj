using System.Text.RegularExpressions;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.UserDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static partial class UserValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegexPattern();

    // Раздел 21 ТЗ (Register): FullName 3–100 символов, Email обязателен и
    // корректен, PhoneNumber обязателен, Password минимум 6 символов при
    // создании. Хэширование — в UserService (BCrypt), сюда всегда приходит
    // raw-пароль, а не хэш (см. audit 2026-07-28, находка 2.1).
    public static Result<string>? ValidateCreate(CreateUserDto dto)
    {
        var common = ValidateCommon(dto.FullName, dto.Email, dto.PhoneNumber, dto.Role);
        if (common is not null)
            return common;

        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<string>.Fail("Пароль обязателен", ErrorType.Validation);

        if (dto.Password.Length < 6)
            return Result<string>.Fail("Пароль должен быть не короче 6 символов", ErrorType.Validation);

        return null;
    }

    // Password необязателен при обновлении — null/пусто означает "не менять".
    // Если передан, применяются те же правила длины, что и при создании.
    public static Result<string>? ValidateUpdate(UpdateUserDto dto)
    {
        var common = ValidateCommon(dto.FullName, dto.Email, dto.PhoneNumber, dto.Role);
        if (common is not null)
            return common;

        if (!string.IsNullOrEmpty(dto.Password) && dto.Password.Length < 6)
            return Result<string>.Fail("Пароль должен быть не короче 6 символов", ErrorType.Validation);

        return null;
    }

    private static Result<string>? ValidateCommon(string fullName, string email, string phoneNumber, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result<string>.Fail("FullName обязателен", ErrorType.Validation);

        if (fullName.Length is < 3 or > 100)
            return Result<string>.Fail("FullName должен быть от 3 до 100 символов", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(email))
            return Result<string>.Fail("Email обязателен", ErrorType.Validation);

        if (!EmailRegexPattern().IsMatch(email))
            return Result<string>.Fail("Email имеет некорректный формат", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result<string>.Fail("PhoneNumber обязателен", ErrorType.Validation);

        if (!Enum.IsDefined(role))
            return Result<string>.Fail("Указана несуществующая роль пользователя", ErrorType.Validation);

        return null;
    }
}
