using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IFavoriteRepository
{
    Task<List<Favorite>> GetAllAsync();
    Task<Favorite?> GetByIdAsync(int id);
    Task AddAsync(Favorite favorite);
    Task UpdateAsync(Favorite favorite);
    Task DeleteAsync(Favorite favorite);
}
