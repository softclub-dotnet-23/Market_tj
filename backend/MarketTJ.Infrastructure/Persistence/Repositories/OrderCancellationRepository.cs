using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class OrderCancellationRepository(AppDbContext context) : IOrderCancellationRepository
{
    public async Task AddAsync(OrderCancellation cancellation)
    {
        await context.OrderCancellations.AddAsync(cancellation);
        await context.SaveChangesAsync();
    }

    public async Task<int> CountSinceAsync(int userId, string role, DateTime since)
        => await context.OrderCancellations
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Role == role && x.CreatedAt >= since)
            .CountAsync();
}
