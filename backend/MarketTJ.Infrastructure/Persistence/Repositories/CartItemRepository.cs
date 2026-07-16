using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class CartItemRepository(AppDbContext context) : ICartItemRepository
{
    public async Task<List<CartItem>> GetAllAsync()
        => await context.CartItems.ToListAsync();

    public async Task<CartItem?> GetByIdAsync(int id)
        => await context.CartItems.FindAsync(id);

    public async Task AddAsync(CartItem cartItem)
    {
        await context.CartItems.AddAsync(cartItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CartItem cartItem)
    {
        context.CartItems.Update(cartItem);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(CartItem cartItem)
    {
        context.CartItems.Remove(cartItem);
        await context.SaveChangesAsync();
    }
}
