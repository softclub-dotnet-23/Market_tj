using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IProductListingRepository
{
    Task<List<ProductListing>> GetAllAsync();
    Task<ProductListing?> GetByIdAsync(int id);

    // Поиск по названию/описанию объявления и названию продукта — нужен для
    // AI-ассистента (tool search_products) и для будущего каталога с поиском
    // (раздел 13.5 ТЗ). Не кэшируется — запрос произвольный, TTL по хэшу
    // запроса усложнил бы метод сверх текущей задачи.
    Task<List<ProductListing>> SearchAsync(string query);

    Task AddAsync(ProductListing productListing);
    Task UpdateAsync(ProductListing productListing);
    Task DeleteAsync(ProductListing productListing);
}
