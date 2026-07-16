using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(Order order);
}
