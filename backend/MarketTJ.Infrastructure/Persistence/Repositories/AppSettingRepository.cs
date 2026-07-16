using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class AppSettingRepository(AppDbContext context) : IAppSettingRepository
{
    public async Task<List<AppSetting>> GetAllAsync()
        => await context.AppSettings.ToListAsync();

    public async Task<AppSetting?> GetByIdAsync(int id)
        => await context.AppSettings.FindAsync(id);

    public async Task AddAsync(AppSetting appSetting)
    {
        await context.AppSettings.AddAsync(appSetting);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppSetting appSetting)
    {
        context.AppSettings.Update(appSetting);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(AppSetting appSetting)
    {
        context.AppSettings.Remove(appSetting);
        await context.SaveChangesAsync();
    }
}
