using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IDeliveryZoneRepository
{
    Task<List<DeliveryZone>> GetAllAsync();
    Task<DeliveryZone?> GetByIdAsync(int id);
    Task AddAsync(DeliveryZone deliveryZone);
    Task UpdateAsync(DeliveryZone deliveryZone);
    Task DeleteAsync(DeliveryZone deliveryZone);
}
