using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IFarmerDocumentRepository
{
    Task<List<FarmerDocument>> GetAllAsync();
    Task<FarmerDocument?> GetByIdAsync(int id);

    // Для проверки "загрузил ли фермер обязательные документы" перед
    // созданием объявления (audit 2026-08-02) — только документы одного
    // профиля, не весь список.
    Task<List<FarmerDocument>> GetByFarmerProfileIdAsync(int farmerProfileId);
    Task AddAsync(FarmerDocument farmerDocument);
    Task UpdateAsync(FarmerDocument farmerDocument);
    Task DeleteAsync(FarmerDocument farmerDocument);
}
