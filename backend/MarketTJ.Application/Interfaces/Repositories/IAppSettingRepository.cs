using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IAppSettingRepository
{
    Task<List<AppSetting>> GetAllAsync();
    Task<AppSetting?> GetByIdAsync(int id);
    Task AddAsync(AppSetting appSetting);
    Task UpdateAsync(AppSetting appSetting);
    Task DeleteAsync(AppSetting appSetting);
}
