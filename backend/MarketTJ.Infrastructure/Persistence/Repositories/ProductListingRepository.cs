using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
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

    // p.Product теперь необязателен (см. ProductListing.ProductId) — новые
    // объявления его не заполняют, поэтому проверка на null обязательна,
    // иначе ILike(p.Product.Name, ...) уронит запрос на NullReferenceException.
    public async Task<List<ProductListing>> SearchAsync(string query)
        => await context.ProductListings
            .Where(p => EF.Functions.ILike(p.Title, $"%{query}%")
                     || (p.Description != null && EF.Functions.ILike(p.Description, $"%{query}%"))
                     || (p.Product != null && EF.Functions.ILike(p.Product.Name, $"%{query}%")))
            .Take(10)
            .ToListAsync();

    // Раздел 13.5 ТЗ: фильтрация/сортировка/пагинация целиком в БД — WHERE по
    // каждому активному фильтру, затем OFFSET/FETCH, а не Skip/Take над уже
    // загруженным в память списком (тот подход остаётся только в GetAllAsync
    // выше — им пользуется админка, которой нужны ВСЕ статусы объявлений).
    // Rating/OrderCount — коррелированные подзапросы (агрегатных колонок нет),
    // общий для всех вариантов сортировки Select, чтобы значения были
    // доступны фронту независимо от того, по какому полю сортируем.
    public async Task<(List<(ProductListing Listing, double Rating, int OrderCount)> Items, int TotalCount)> SearchCatalogAsync(ProductListingSearchFilter filter)
    {
        // Как и в catalogStore.ts (публичная витрина, теперь заменяется этим
        // эндпоинтом): показываем только объявления подтверждённых фермеров —
        // иначе покупатель увидел бы товар продавца, ещё не прошедшего проверку.
        var query = context.ProductListings
            .Where(x => x.Status == ListingStatus.Active
                     && x.AvailableQuantity > 0
                     && x.FarmerProfile.VerificationStatus == FarmerVerificationStatus.Verified);

        if (filter.CategoryIds is { Count: > 0 })
            query = query.Where(x => filter.CategoryIds.Contains(x.CategoryId));

        if (!string.IsNullOrWhiteSpace(filter.Region))
            query = query.Where(x => x.Region == filter.Region);

        if (filter.FarmerProfileId.HasValue)
            query = query.Where(x => x.FarmerProfileId == filter.FarmerProfileId.Value);

        if (filter.PriceMin.HasValue)
            query = query.Where(x => x.RetailPricePerKg >= filter.PriceMin.Value);

        if (filter.PriceMax.HasValue)
            query = query.Where(x => x.RetailPricePerKg <= filter.PriceMax.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search;
            query = query.Where(x => EF.Functions.ILike(x.Title, $"%{term}%")
                                   || (x.Description != null && EF.Functions.ILike(x.Description, $"%{term}%")));
        }

        if (filter.ListingIds is { Count: > 0 })
            query = query.Where(x => filter.ListingIds.Contains(x.Id));

        var totalCount = await query.CountAsync();

        var withStats = query.Select(x => new
        {
            Listing = x,
            Rating = context.Reviews.Where(r => r.FarmerId == x.FarmerProfileId).Average(r => (double?)r.Rating) ?? 0,
            OrderCount = context.OrderItems.Count(oi => oi.ProductListingId == x.Id),
        });

        var ordered = filter.SortBy switch
        {
            "price-asc" => withStats.OrderBy(x => x.Listing.RetailPricePerKg),
            "price-desc" => withStats.OrderByDescending(x => x.Listing.RetailPricePerKg),
            "rating" => withStats.OrderByDescending(x => x.Rating),
            "fresh" => withStats.OrderByDescending(x => x.Listing.CreatedAt),
            _ => withStats.OrderByDescending(x => x.OrderCount),
        };

        var page = await ordered
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (page.Select(x => (x.Listing, x.Rating, x.OrderCount)).ToList(), totalCount);
    }

    public async Task<List<string>> GetDistinctActiveRegionsAsync()
        => await context.ProductListings
            .Where(x => x.Status == ListingStatus.Active
                     && x.AvailableQuantity > 0
                     && x.FarmerProfile.VerificationStatus == FarmerVerificationStatus.Verified)
            .Select(x => x.Region)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync();

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
