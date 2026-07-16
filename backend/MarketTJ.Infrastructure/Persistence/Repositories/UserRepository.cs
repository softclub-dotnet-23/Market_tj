using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<List<User>> GetAllAsync()
        => await context.Users.ToListAsync();

    public async Task<User?> GetByIdAsync(int id)
        => await context.Users.FindAsync(id);

    public async Task AddAsync(User user)
    {
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }
}
