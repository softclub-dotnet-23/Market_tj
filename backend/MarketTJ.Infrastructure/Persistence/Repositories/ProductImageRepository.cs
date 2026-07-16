using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class ProductImageRepository(AppDbContext context) : IProductImageRepository
{
    public async Task<List<ProductImage>> GetAllAsync()
        => await context.ProductImages.ToListAsync();

    public async Task<ProductImage?> GetByIdAsync(int id)
        => await context.ProductImages.FindAsync(id);

    public async Task AddAsync(ProductImage productImage)
    {
        await context.ProductImages.AddAsync(productImage);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductImage productImage)
    {
        context.ProductImages.Update(productImage);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ProductImage productImage)
    {
        context.ProductImages.Remove(productImage);
        await context.SaveChangesAsync();
    }
}
