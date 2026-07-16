using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface ICourierProfileRepository
{
    Task<List<CourierProfile>> GetAllAsync();
    Task<CourierProfile?> GetByIdAsync(int id);
    Task AddAsync(CourierProfile courierProfile);
    Task UpdateAsync(CourierProfile courierProfile);
    Task DeleteAsync(CourierProfile courierProfile);
}
