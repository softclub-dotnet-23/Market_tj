using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class FarmerProfileRepository(AppDbContext context) : IFarmerProfileRepository
{
    public async Task<List<FarmerProfile>> GetAllAsync()
        => await context.FarmerProfiles.ToListAsync();

    public async Task<FarmerProfile?> GetByIdAsync(int id)
        => await context.FarmerProfiles.FindAsync(id);

    public async Task<FarmerProfile?> GetByUserIdAsync(int userId)
        => await context.FarmerProfiles.FirstOrDefaultAsync(x => x.UserId == userId);

    public async Task AddAsync(FarmerProfile farmerProfile)
    {
        await context.FarmerProfiles.AddAsync(farmerProfile);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FarmerProfile farmerProfile)
    {
        context.FarmerProfiles.Update(farmerProfile);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(FarmerProfile farmerProfile)
    {
        context.FarmerProfiles.Remove(farmerProfile);
        await context.SaveChangesAsync();
    }
}
