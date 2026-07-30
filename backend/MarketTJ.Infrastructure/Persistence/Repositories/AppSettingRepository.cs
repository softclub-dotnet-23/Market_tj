using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class AppSettingRepository(AppDbContext context, ICacheService cache) : IAppSettingRepository
{
    // Настройки читаются часто (в т.ч. на каждый GET /api/admin/settings),
    // а меняются редко — кэшируем весь список целиком, как ProductListing/Category.
    private const string AllSettingsCacheKey = "app-settings:all";

    public async Task<List<AppSetting>> GetAllAsync()
    {
        var cached = await cache.GetAsync<List<AppSetting>>(AllSettingsCacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var settings = await context.AppSettings.ToListAsync();
        await cache.SetAsync(AllSettingsCacheKey, settings, TimeSpan.FromMinutes(30));
        return settings;
    }

    public async Task<AppSetting?> GetByIdAsync(int id)
        => await context.AppSettings.FindAsync(id);

    public async Task AddAsync(AppSetting appSetting)
    {
        await context.AppSettings.AddAsync(appSetting);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllSettingsCacheKey);
    }

    public async Task UpdateAsync(AppSetting appSetting)
    {
        context.AppSettings.Update(appSetting);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllSettingsCacheKey);
    }

    public async Task DeleteAsync(AppSetting appSetting)
    {
        context.AppSettings.Remove(appSetting);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllSettingsCacheKey);
    }
}
