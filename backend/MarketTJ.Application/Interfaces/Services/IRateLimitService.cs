using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

// Блок 3 (2026-08-08, по явному запросу пользователя) — общий, переиспользуемый
// механизм защиты от спам-кликов: не привязан к конкретной кнопке/эндпоинту,
// вызывающий код (RateLimitAttribute в WebApi) передаёт endpointKey/лимиты сам.
public interface IRateLimitService
{
    // Result.Fail(..., ErrorType.TooManyRequests) — запрос запрещён (либо уже
    // есть активный бан, либо этим вызовом лимит только что превышен), текст
    // ошибки уже содержит время разблокировки, готов к показу пользователю.
    // Result.Ok(null) — запрос разрешён.
    Task<Result<string?>> CheckAsync(int userId, string role, string endpointKey, int maxRequests, TimeSpan window);
}
