using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.AuthDto;

public class RegisterRequestDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Password { get; set; } = null!;

    // Самостоятельная регистрация доступна Customer/Farmer/Courier (раздел 23
    // ТЗ + расширение по прямому запросу пользователя для Courier). Admin —
    // только через AdminSeeder, самостоятельная регистрация недоступна.
    public UserRole Role { get; set; }
}
