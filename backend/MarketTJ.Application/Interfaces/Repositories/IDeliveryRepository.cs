using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IDeliveryRepository
{
    Task<List<Delivery>> GetAllAsync();
    Task<Delivery?> GetByIdAsync(int id);
    Task AddAsync(Delivery delivery);
    Task UpdateAsync(Delivery delivery);
    Task DeleteAsync(Delivery delivery);
}
