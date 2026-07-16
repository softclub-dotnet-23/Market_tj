using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class ReviewRepository(AppDbContext context) : IReviewRepository
{
    public async Task<List<Review>> GetAllAsync()
        => await context.Reviews.ToListAsync();

    public async Task<Review?> GetByIdAsync(int id)
        => await context.Reviews.FindAsync(id);

    public async Task AddAsync(Review review)
    {
        await context.Reviews.AddAsync(review);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Review review)
    {
        context.Reviews.Update(review);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Review review)
    {
        context.Reviews.Remove(review);
        await context.SaveChangesAsync();
    }
}
