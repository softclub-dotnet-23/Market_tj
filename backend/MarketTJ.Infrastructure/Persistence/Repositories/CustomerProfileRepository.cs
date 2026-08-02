using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class CustomerProfileRepository(AppDbContext context) : ICustomerProfileRepository
{
    public async Task<List<CustomerProfile>> GetAllAsync()
        => await context.CustomerProfiles.AsNoTracking().ToListAsync();

    public async Task<CustomerProfile?> GetByIdAsync(int id)
        => await context.CustomerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<CustomerProfile?> GetByUserIdAsync(int userId)
        => await context.CustomerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);

    public async Task AddAsync(CustomerProfile customerProfile)
    {
        await context.CustomerProfiles.AddAsync(customerProfile);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerProfile customerProfile)
    {
        context.CustomerProfiles.Update(customerProfile);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(CustomerProfile customerProfile)
    {
        context.CustomerProfiles.Remove(customerProfile);
        await context.SaveChangesAsync();
    }
}
