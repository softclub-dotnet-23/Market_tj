using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface ICourierProfileRepository
{
    Task<List<CourierProfile>> GetAllAsync();
    Task<CourierProfile?> GetByIdAsync(int id);

    // Раньше отсутствовал — DeliveryService.IsOwnerAsync резолвил владение
    // курьера сканированием всего GetAllAsync() (см. audit 2026-08-02).
    Task<CourierProfile?> GetByUserIdAsync(int userId);

    Task AddAsync(CourierProfile courierProfile);
    Task UpdateAsync(CourierProfile courierProfile);
    Task DeleteAsync(CourierProfile courierProfile);
}
