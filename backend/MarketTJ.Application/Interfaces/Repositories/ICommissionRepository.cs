using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface ICommissionRepository
{
    Task<List<Commission>> GetAllAsync();
    Task<Commission?> GetByIdAsync(int id);
    Task AddAsync(Commission commission);
    Task UpdateAsync(Commission commission);
    Task DeleteAsync(Commission commission);
}
