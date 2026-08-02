using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IProductImageRepository
{
    Task<List<ProductImage>> GetAllAsync();
    Task<ProductImage?> GetByIdAsync(int id);

    // Для обогащения ответа /product-listings изображениями без отдельного
    // похода фронта на /product-images за ВСЕМИ картинками сайта (audit
    // 2026-08-02) — вызывающий сервис передаёт только id объявлений текущей
    // страницы, не весь каталог.
    Task<List<ProductImage>> GetByListingIdsAsync(List<int> listingIds);
    Task AddAsync(ProductImage productImage);
    Task UpdateAsync(ProductImage productImage);
    Task DeleteAsync(ProductImage productImage);
}
