using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class DailySalesSnapshotRepository(AppDbContext context) : IDailySalesSnapshotRepository
{
    public async Task<List<DailySalesSnapshot>> GetAllAsync()
        => await context.DailySalesSnapshots.ToListAsync();

    public async Task<DailySalesSnapshot?> GetByIdAsync(int id)
        => await context.DailySalesSnapshots.FindAsync(id);

    public async Task AddAsync(DailySalesSnapshot dailySalesSnapshot)
    {
        await context.DailySalesSnapshots.AddAsync(dailySalesSnapshot);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(DailySalesSnapshot dailySalesSnapshot)
    {
        context.DailySalesSnapshots.Update(dailySalesSnapshot);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(DailySalesSnapshot dailySalesSnapshot)
    {
        context.DailySalesSnapshots.Remove(dailySalesSnapshot);
        await context.SaveChangesAsync();
    }
}
