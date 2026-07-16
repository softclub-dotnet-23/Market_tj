using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IDeliverySlotRepository
{
    Task<List<DeliverySlot>> GetAllAsync();
    Task<DeliverySlot?> GetByIdAsync(int id);
    Task AddAsync(DeliverySlot deliverySlot);
    Task UpdateAsync(DeliverySlot deliverySlot);
    Task DeleteAsync(DeliverySlot deliverySlot);
}
