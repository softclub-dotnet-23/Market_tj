using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext context) : IOrderRepository
{
    public async Task<List<Order>> GetAllAsync()
        => await context.Orders.ToListAsync();

    public async Task<Order?> GetByIdAsync(int id)
        => await context.Orders.FindAsync(id);

    public async Task AddAsync(Order order)
    {
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        context.Orders.Update(order);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Order order)
    {
        context.Orders.Remove(order);
        await context.SaveChangesAsync();
    }
}
