using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class CommissionRepository(AppDbContext context) : ICommissionRepository
{
    public async Task<List<Commission>> GetAllAsync()
        => await context.Commissions.ToListAsync();

    public async Task<Commission?> GetByIdAsync(int id)
        => await context.Commissions.FindAsync(id);

    public async Task AddAsync(Commission commission)
    {
        await context.Commissions.AddAsync(commission);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Commission commission)
    {
        context.Commissions.Update(commission);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Commission commission)
    {
        context.Commissions.Remove(commission);
        await context.SaveChangesAsync();
    }
}
