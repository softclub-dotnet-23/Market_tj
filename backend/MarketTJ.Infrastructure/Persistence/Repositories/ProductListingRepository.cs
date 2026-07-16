using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class ProductListingRepository(AppDbContext context, ICacheService cache) : IProductListingRepository
{
    // Кэшируется общий список (уважает глобальный soft-delete query filter).
    // Публичный каталог с фильтрами/пагинацией (раздел 13.5 ТЗ) появится на
    // уровне Application-сервиса позже (Этап 4, раздел 23) — там же будет
    // кэш по конкретному набору фильтров, а не общий список.
    private const string AllListingsCacheKey = "product-listings:all";

    public async Task<List<ProductListing>> GetAllAsync()
    {
        var cached = await cache.GetAsync<List<ProductListing>>(AllListingsCacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var listings = await context.ProductListings.ToListAsync();
        await cache.SetAsync(AllListingsCacheKey, listings, TimeSpan.FromMinutes(10));
        return listings;
    }

    public async Task<ProductListing?> GetByIdAsync(int id)
        => await context.ProductListings.FindAsync(id);

    public async Task AddAsync(ProductListing productListing)
    {
        await context.ProductListings.AddAsync(productListing);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllListingsCacheKey);
    }

    public async Task UpdateAsync(ProductListing productListing)
    {
        context.ProductListings.Update(productListing);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllListingsCacheKey);
    }

    public async Task DeleteAsync(ProductListing productListing)
    {
        context.ProductListings.Remove(productListing);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllListingsCacheKey);
    }
}
