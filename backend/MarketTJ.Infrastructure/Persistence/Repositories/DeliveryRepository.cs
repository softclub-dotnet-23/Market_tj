using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class DeliveryRepository(AppDbContext context) : IDeliveryRepository
{
    public async Task<List<Delivery>> GetAllAsync()
        => await context.Deliveries.AsNoTracking().ToListAsync();

    public async Task<Delivery?> GetByIdAsync(int id)
        => await context.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Delivery?> GetByOrderIdAsync(int orderId)
        => await context.Deliveries.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == orderId);

    public async Task<List<Delivery>> GetByCourierIdAsync(int courierId)
        => await context.Deliveries
            .AsNoTracking()
            .Where(x => x.CourierId == courierId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

    public async Task<Dictionary<int, int>> GetActiveCountsByCourierIdsAsync(List<int> courierIds)
        => await context.Deliveries
            .AsNoTracking()
            .Where(x => x.CourierId != null && courierIds.Contains(x.CourierId.Value)
                     && x.Status != DeliveryStatus.Delivered && x.Status != DeliveryStatus.Cancelled)
            .GroupBy(x => x.CourierId!.Value)
            .Select(g => new { CourierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourierId, x => x.Count);

    public async Task<Dictionary<int, int>> GetCompletedCountsByCourierIdsAsync(List<int> courierIds)
        => await context.Deliveries
            .AsNoTracking()
            .Where(x => x.CourierId != null && courierIds.Contains(x.CourierId.Value) && x.Status == DeliveryStatus.Delivered)
            .GroupBy(x => x.CourierId!.Value)
            .Select(g => new { CourierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourierId, x => x.Count);

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
