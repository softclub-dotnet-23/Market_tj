using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IReviewRepository
{
    Task<List<Review>> GetAllAsync();
    Task<Review?> GetByIdAsync(int id);

    // Средний рейтинг + число отзывов по каждому фермеру — для публичной
    // витрины фермеров (/farmer-profiles/public), только для переданных id,
    // не для всего каталога (audit 2026-08-02).
    Task<Dictionary<int, (double Rating, int Count)>> GetRatingStatsByFarmerIdsAsync(List<int> farmerIds);
    Task AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(Review review);
}
