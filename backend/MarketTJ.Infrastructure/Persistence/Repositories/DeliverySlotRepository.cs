using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class DeliverySlotRepository(AppDbContext context) : IDeliverySlotRepository
{
    public async Task<List<DeliverySlot>> GetAllAsync()
        => await context.DeliverySlots.ToListAsync();

    public async Task<DeliverySlot?> GetByIdAsync(int id)
        => await context.DeliverySlots.FindAsync(id);

    public async Task AddAsync(DeliverySlot deliverySlot)
    {
        await context.DeliverySlots.AddAsync(deliverySlot);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DeliverySlot deliverySlot)
    {
        context.DeliverySlots.Update(deliverySlot);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(DeliverySlot deliverySlot)
    {
        context.DeliverySlots.Remove(deliverySlot);
        await context.SaveChangesAsync();
    }
}
