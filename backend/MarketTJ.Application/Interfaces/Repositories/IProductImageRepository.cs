using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IProductImageRepository
{
    Task<List<ProductImage>> GetAllAsync();
    Task<ProductImage?> GetByIdAsync(int id);
    Task AddAsync(ProductImage productImage);
    Task UpdateAsync(ProductImage productImage);
    Task DeleteAsync(ProductImage productImage);
}
