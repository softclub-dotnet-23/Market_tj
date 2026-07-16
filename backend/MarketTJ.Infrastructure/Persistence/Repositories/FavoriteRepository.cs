using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class FavoriteRepository(AppDbContext context) : IFavoriteRepository
{
    public async Task<List<Favorite>> GetAllAsync()
        => await context.Favorites.ToListAsync();

    public async Task<Favorite?> GetByIdAsync(int id)
        => await context.Favorites.FindAsync(id);

    public async Task AddAsync(Favorite favorite)
    {
        await context.Favorites.AddAsync(favorite);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Favorite favorite)
    {
        context.Favorites.Update(favorite);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Favorite favorite)
    {
        context.Favorites.Remove(favorite);
        await context.SaveChangesAsync();
    }
}
