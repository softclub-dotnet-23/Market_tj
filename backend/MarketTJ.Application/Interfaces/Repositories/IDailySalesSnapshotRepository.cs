using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IDailySalesSnapshotRepository
{
    Task<List<DailySalesSnapshot>> GetAllAsync();
    Task<DailySalesSnapshot?> GetByIdAsync(int id);
    Task AddAsync(DailySalesSnapshot dailySalesSnapshot);
    Task UpdateAsync(DailySalesSnapshot dailySalesSnapshot);
    Task DeleteAsync(DailySalesSnapshot dailySalesSnapshot);
}
