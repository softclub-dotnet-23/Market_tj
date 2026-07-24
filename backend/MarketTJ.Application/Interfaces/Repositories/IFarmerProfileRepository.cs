using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IFarmerProfileRepository
{
    Task<List<FarmerProfile>> GetAllAsync();
    Task<FarmerProfile?> GetByIdAsync(int id);
    Task<FarmerProfile?> GetByUserIdAsync(int userId);
    Task AddAsync(FarmerProfile farmerProfile);
    Task UpdateAsync(FarmerProfile farmerProfile);
    Task DeleteAsync(FarmerProfile farmerProfile);
}
