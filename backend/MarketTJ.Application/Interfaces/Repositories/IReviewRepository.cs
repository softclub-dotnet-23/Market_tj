using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IReviewRepository
{
    Task<List<Review>> GetAllAsync();
    Task<Review?> GetByIdAsync(int id);
    Task AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(Review review);
}
