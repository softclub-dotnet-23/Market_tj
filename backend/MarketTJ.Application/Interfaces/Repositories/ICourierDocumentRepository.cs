using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface ICourierDocumentRepository
{
    Task<List<CourierDocument>> GetAllAsync();
    Task<CourierDocument?> GetByIdAsync(int id);
    Task<List<CourierDocument>> GetByCourierProfileIdAsync(int courierProfileId);
    Task AddAsync(CourierDocument courierDocument);
    Task UpdateAsync(CourierDocument courierDocument);
    Task DeleteAsync(CourierDocument courierDocument);
}
