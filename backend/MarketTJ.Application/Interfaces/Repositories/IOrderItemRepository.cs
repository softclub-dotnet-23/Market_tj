using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IOrderItemRepository
{
    Task<List<OrderItem>> GetAllAsync();
    Task<OrderItem?> GetByIdAsync(int id);
    Task AddAsync(OrderItem orderItem);
    Task UpdateAsync(OrderItem orderItem);
    Task DeleteAsync(OrderItem orderItem);
}
