using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface ICartItemRepository
{
    Task<List<CartItem>> GetAllAsync();
    Task<CartItem?> GetByIdAsync(int id);
    Task AddAsync(CartItem cartItem);
    Task UpdateAsync(CartItem cartItem);
    Task DeleteAsync(CartItem cartItem);
}
