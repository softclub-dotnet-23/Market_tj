using System.Text.RegularExpressions;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static partial class AuthValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegexPattern();

    // Раздел 23 ТЗ (Этап 2): самостоятельная регистрация — только Customer/Farmer.
    public static Result<string>? ValidateRegister(RegisterRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Result<string>.Fail("FullName обязателен", ErrorType.Validation);

        if (dto.FullName.Length is < 3 or > 100)
            return Result<string>.Fail("FullName должен быть от 3 до 100 символов", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<string>.Fail("Email обязателен", ErrorType.Validation);

        if (!EmailRegexPattern().IsMatch(dto.Email))
            return Result<string>.Fail("Email имеет некорректный формат", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            return Result<string>.Fail("PhoneNumber обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(dto.Password))
            return Result<string>.Fail("Пароль обязателен", ErrorType.Validation);

        if (dto.Password.Length < 6)
            return Result<string>.Fail("Пароль должен быть не короче 6 символов", ErrorType.Validation);

        if (dto.Role is not (UserRole.Customer or UserRole.Farmer))
            return Result<string>.Fail("Самостоятельная регистрация доступна только для роли Customer или Farmer", ErrorType.Validation);

        return null;
    }
}
