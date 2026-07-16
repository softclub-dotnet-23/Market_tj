using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public async Task<List<Notification>> GetAllAsync()
        => await context.Notifications.ToListAsync();

    public async Task<Notification?> GetByIdAsync(int id)
        => await context.Notifications.FindAsync(id);

    public async Task AddAsync(Notification notification)
    {
        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Notification notification)
    {
        context.Notifications.Update(notification);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Notification notification)
    {
        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();
    }
}
