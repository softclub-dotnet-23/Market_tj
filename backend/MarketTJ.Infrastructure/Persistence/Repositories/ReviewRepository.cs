using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class ReviewRepository(AppDbContext context) : IReviewRepository
{
    public async Task<List<Review>> GetAllAsync()
        => await context.Reviews.AsNoTracking().ToListAsync();

    public async Task<Review?> GetByIdAsync(int id)
        => await context.Reviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Dictionary<int, (double Rating, int Count)>> GetRatingStatsByFarmerIdsAsync(List<int> farmerIds)
    {
        var rows = await context.Reviews
            .AsNoTracking()
            .Where(r => farmerIds.Contains(r.FarmerId))
            .GroupBy(r => r.FarmerId)
            .Select(g => new { FarmerId = g.Key, Rating = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync();

        return rows.ToDictionary(x => x.FarmerId, x => (Math.Round(x.Rating, 1), x.Count));
    }

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
