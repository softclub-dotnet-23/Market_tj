using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IFarmerDocumentRepository
{
    Task<List<FarmerDocument>> GetAllAsync();
    Task<FarmerDocument?> GetByIdAsync(int id);
    Task AddAsync(FarmerDocument farmerDocument);
    Task UpdateAsync(FarmerDocument farmerDocument);
    Task DeleteAsync(FarmerDocument farmerDocument);
}
