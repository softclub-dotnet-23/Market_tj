namespace MarketTJ.Application.Interfaces.Services;

// Раздел 16 ТЗ: "не принимать UserId из клиента, если его можно получить из
// токена". Реализация — в WebApi (MarketTJ.WebApi/Services/CurrentUserService),
// т.к. читает HttpContext, а это ASP.NET Core-specific концепция, до которой
// Application-слой не должен дотягиваться напрямую.
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Role { get; }
    string? Email { get; }
}
