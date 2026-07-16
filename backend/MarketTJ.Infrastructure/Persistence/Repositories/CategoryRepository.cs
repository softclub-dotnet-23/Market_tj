using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class CategoryRepository(AppDbContext context, ICacheService cache) : ICategoryRepository
{
    private const string AllCategoriesCacheKey = "categories:all";

    public async Task<List<Category>> GetAllAsync()
    {
        var cached = await cache.GetAsync<List<Category>>(AllCategoriesCacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var categories = await context.Categories.ToListAsync();
        await cache.SetAsync(AllCategoriesCacheKey, categories, TimeSpan.FromMinutes(30));
        return categories;
    }

    public async Task<Category?> GetByIdAsync(int id)
        => await context.Categories.FindAsync(id);

    public async Task AddAsync(Category category)
    {
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllCategoriesCacheKey);
    }

    public async Task UpdateAsync(Category category)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllCategoriesCacheKey);
    }

    public async Task DeleteAsync(Category category)
    {
        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        await cache.RemoveAsync(AllCategoriesCacheKey);
    }
}
