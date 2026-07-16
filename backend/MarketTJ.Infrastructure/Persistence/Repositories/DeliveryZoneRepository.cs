using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class DeliveryZoneRepository(AppDbContext context) : IDeliveryZoneRepository
{
    public async Task<List<DeliveryZone>> GetAllAsync()
        => await context.DeliveryZones.ToListAsync();

    public async Task<DeliveryZone?> GetByIdAsync(int id)
        => await context.DeliveryZones.FindAsync(id);

    public async Task AddAsync(DeliveryZone deliveryZone)
    {
        await context.DeliveryZones.AddAsync(deliveryZone);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DeliveryZone deliveryZone)
    {
        context.DeliveryZones.Update(deliveryZone);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(DeliveryZone deliveryZone)
    {
        context.DeliveryZones.Remove(deliveryZone);
        await context.SaveChangesAsync();
    }
}
