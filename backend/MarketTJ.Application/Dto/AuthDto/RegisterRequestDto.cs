using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.AuthDto;

public class RegisterRequestDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Password { get; set; } = null!;

    // Самостоятельная регистрация доступна только Customer/Farmer (раздел 23
    // ТЗ, Этап 2: "регистрация Customer; регистрация Farmer"). Admin — только
    // через AdminSeeder, Courier — назначается администратором.
    public UserRole Role { get; set; }
}
