using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class DeliveryRepository(AppDbContext context) : IDeliveryRepository
{
    public async Task<List<Delivery>> GetAllAsync()
        => await context.Deliveries.ToListAsync();

    public async Task<Delivery?> GetByIdAsync(int id)
        => await context.Deliveries.FindAsync(id);

    public async Task AddAsync(Delivery delivery)
    {
        await context.Deliveries.AddAsync(delivery);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Delivery delivery)
    {
        context.Deliveries.Update(delivery);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Delivery delivery)
    {
        context.Deliveries.Remove(delivery);
        await context.SaveChangesAsync();
    }
}
