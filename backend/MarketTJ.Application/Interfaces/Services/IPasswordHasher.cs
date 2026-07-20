namespace MarketTJ.Application.Interfaces.Services;

// Абстракция над BCrypt (пакет BCrypt.Net-Next доступен только в Infrastructure,
// Application на него не ссылается — раздел 20 ТЗ, Clean Architecture).
public interface IPasswordHasher
{
    bool Verify(string password, string passwordHash);
}
