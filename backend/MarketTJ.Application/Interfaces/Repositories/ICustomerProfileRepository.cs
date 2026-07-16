using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface ICustomerProfileRepository
{
    Task<List<CustomerProfile>> GetAllAsync();
    Task<CustomerProfile?> GetByIdAsync(int id);
    Task AddAsync(CustomerProfile customerProfile);
    Task UpdateAsync(CustomerProfile customerProfile);
    Task DeleteAsync(CustomerProfile customerProfile);
}
