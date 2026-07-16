using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class OrderItemRepository(AppDbContext context) : IOrderItemRepository
{
    public async Task<List<OrderItem>> GetAllAsync()
        => await context.OrderItems.ToListAsync();

    public async Task<OrderItem?> GetByIdAsync(int id)
        => await context.OrderItems.FindAsync(id);

    public async Task AddAsync(OrderItem orderItem)
    {
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OrderItem orderItem)
    {
        context.OrderItems.Update(orderItem);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(OrderItem orderItem)
    {
        context.OrderItems.Remove(orderItem);
        await context.SaveChangesAsync();
    }
}
