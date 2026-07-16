using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetAllAsync();
    Task<Notification?> GetByIdAsync(int id);
    Task AddAsync(Notification notification);
    Task UpdateAsync(Notification notification);
    Task DeleteAsync(Notification notification);
}
