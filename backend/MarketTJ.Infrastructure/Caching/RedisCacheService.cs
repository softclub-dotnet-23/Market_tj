using System.Text.Json;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace MarketTJ.Infrastructure.Caching;

public class RedisCacheService(IDistributedCache cache) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await cache.GetStringAsync(key);
        return data is null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.SetAbsoluteExpiration(expiration.Value);
        }

        var data = JsonSerializer.Serialize(value);
        await cache.SetStringAsync(key, data, options);
    }

    public async Task RemoveAsync(string key)
        => await cache.RemoveAsync(key);
}
