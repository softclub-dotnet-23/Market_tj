using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class CourierProfileRepository(AppDbContext context) : ICourierProfileRepository
{
    public async Task<List<CourierProfile>> GetAllAsync()
        => await context.CourierProfiles.ToListAsync();

    public async Task<CourierProfile?> GetByIdAsync(int id)
        => await context.CourierProfiles.FindAsync(id);

    public async Task AddAsync(CourierProfile courierProfile)
    {
        await context.CourierProfiles.AddAsync(courierProfile);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CourierProfile courierProfile)
    {
        context.CourierProfiles.Update(courierProfile);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(CourierProfile courierProfile)
    {
        context.CourierProfiles.Remove(courierProfile);
        await context.SaveChangesAsync();
    }
}
