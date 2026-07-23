using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);

    // Отдельная случайная строка (не JWT) — хранится в БД (RefreshToken),
    // обменивается на новую пару access+refresh токенов через /api/auth/refresh.
    string GenerateRefreshToken();

    // Application-слой не читает IConfiguration напрямую (раздел 12 ТЗ:
    // слоистая архитектура) — время жизни токенов приходит отсюда, а не
    // из голой конфигурации, чтобы AuthService не знал о деталях Jwt-секции.
    int AccessTokenExpiryMinutes { get; }
    int RefreshTokenExpiryDays { get; }
}
