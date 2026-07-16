using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IProductListingRepository
{
    Task<List<ProductListing>> GetAllAsync();
    Task<ProductListing?> GetByIdAsync(int id);
    Task AddAsync(ProductListing productListing);
    Task UpdateAsync(ProductListing productListing);
    Task DeleteAsync(ProductListing productListing);
}
