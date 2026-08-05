using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext context) : IOrderRepository
{
    public async Task<List<Order>> GetAllAsync()
        => await context.Orders.AsNoTracking().ToListAsync();

    public async Task<Order?> GetByIdAsync(int id)
        => await context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task AddAsync(Order order)
    {
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        // GetByIdAsync — AsNoTracking, поэтому один и тот же заказ может быть
        // загружен и сохранён дважды за один HTTP-запрос разными вызовами
        // (например DeliveryService сохраняет order.CourierStatus, затем зовёт
        // OrderService.CompleteAfterDeliveryAsync, который грузит заказ заново) —
        // без отсоединения старого инстанса EF бросает "cannot be tracked
        // because another instance with the same key value is already being
        // tracked" на второй попытке в рамках того же scoped DbContext.
        var tracked = context.ChangeTracker.Entries<Order>()
            .FirstOrDefault(e => e.Entity.Id == order.Id && !ReferenceEquals(e.Entity, order));
        if (tracked is not null)
            tracked.State = EntityState.Detached;

        context.Orders.Update(order);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Order order)
    {
        context.Orders.Remove(order);
        await context.SaveChangesAsync();
    }
}
