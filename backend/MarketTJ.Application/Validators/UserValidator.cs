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

    public static Result<string>? ValidateCreate(CreateUserDto dto)
        => Validate(dto.FullName, dto.Email, dto.PhoneNumber, dto.PasswordHash, dto.Role);

    public static Result<string>? ValidateUpdate(UpdateUserDto dto)
        => Validate(dto.FullName, dto.Email, dto.PhoneNumber, dto.PasswordHash, dto.Role);

    // Раздел 21 ТЗ (Register): FullName 3–100 символов, Email обязателен и
    // корректен, PhoneNumber обязателен, Password минимум 6 символов
    // (применено к PasswordHash — в проекте ещё нет отдельного сырого поля
    // Password, см. backend/progress).
    private static Result<string>? Validate(string fullName, string email, string phoneNumber, string passwordHash, UserRole role)
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

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result<string>.Fail("Пароль обязателен", ErrorType.Validation);

        if (passwordHash.Length < 6)
            return Result<string>.Fail("Пароль должен быть не короче 6 символов", ErrorType.Validation);

        if (!Enum.IsDefined(role))
            return Result<string>.Fail("Указана несуществующая роль пользователя", ErrorType.Validation);

        return null;
    }
}
