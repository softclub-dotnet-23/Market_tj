using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Блок 3 (2026-08-08, по явному запросу пользователя) — общий anti-spam
// механизм, переиспользует таблицу AccountBlock/CreateBlockAsync Блока 2
// (BlockType=RateLimit), но со своей, гораздо более короткой парой
// длительностей — здесь "жмёт кнопку слишком быстро", не "систематически
// нарушает бизнес-правила", наказание соразмерно мягче.
public class RateLimitService(
    IMemoryCache cache,
    IAccountBlockService blockService,
    ILogger<RateLimitService> logger) : IRateLimitService
{
    private static readonly TimeSpan FirstOffenseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EscalatedDuration = TimeSpan.FromMinutes(30);

    public async Task<Result<string?>> CheckAsync(int userId, string role, string endpointKey, int maxRequests, TimeSpan window)
    {
        // Уже заблокирован (этим или любым другим механизмом — см.
        // AccountBlockService.GetActiveBlockAsync) — не даём даже попытаться
        // снова, счётчик запросов при этом не трогаем.
        var activeBlock = await blockService.GetActiveBlockAsync(userId);
        if (activeBlock is not null)
            return Result<string?>.Fail(activeBlock.FormatBlockMessage(), ErrorType.TooManyRequests);

        var cacheKey = $"rate-limit:{userId}:{endpointKey}";
        var timestamps = cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = window;
            return new List<DateTime>();
        })!;

        int countInWindow;
        var now = DateTime.UtcNow;
        lock (timestamps)
        {
            timestamps.RemoveAll(t => now - t > window);
            timestamps.Add(now);
            countInWindow = timestamps.Count;
        }

        if (countInWindow <= maxRequests)
            return Result<string?>.Ok(null);

        lock (timestamps) timestamps.Clear();

        var reason = $"Более {maxRequests} запросов к \"{endpointKey}\" за {window.TotalSeconds:0} секунд";
        var block = await blockService.CreateBlockAsync(
            userId, role, AccountBlockService.RateLimitBlockType, reason,
            firstOffenseDuration: FirstOffenseDuration, escalatedDuration: EscalatedDuration);

        logger.LogWarning("Rate limit сработал для пользователя {UserId} на {EndpointKey}: {Reason}", userId, endpointKey, reason);

        return Result<string?>.Fail(block.FormatBlockMessage(), ErrorType.TooManyRequests);
    }
}
