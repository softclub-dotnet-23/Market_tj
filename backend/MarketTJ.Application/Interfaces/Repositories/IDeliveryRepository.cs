using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IDeliveryRepository
{
    Task<List<Delivery>> GetAllAsync();
    Task<Delivery?> GetByIdAsync(int id);
    Task<Delivery?> GetByOrderIdAsync(int orderId);
    Task<List<Delivery>> GetByCourierIdAsync(int courierId);

    // Для карточек курьеров в drawer'е назначения (audit 2026-08-02) — не
    // храним счётчики отдельными колонками (риск рассинхронизации), считаем
    // на лету по переданному набору courierId.
    Task<Dictionary<int, int>> GetActiveCountsByCourierIdsAsync(List<int> courierIds);
    Task<Dictionary<int, int>> GetCompletedCountsByCourierIdsAsync(List<int> courierIds);

    Task AddAsync(Delivery delivery);
    Task UpdateAsync(Delivery delivery);
    Task DeleteAsync(Delivery delivery);
}
