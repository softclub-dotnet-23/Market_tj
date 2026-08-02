using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class FarmerDocumentRepository(AppDbContext context) : IFarmerDocumentRepository
{
    public async Task<List<FarmerDocument>> GetAllAsync()
        => await context.FarmerDocuments.AsNoTracking().ToListAsync();

    public async Task<FarmerDocument?> GetByIdAsync(int id)
        => await context.FarmerDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<List<FarmerDocument>> GetByFarmerProfileIdAsync(int farmerProfileId)
        => await context.FarmerDocuments
            .AsNoTracking()
            .Where(x => x.FarmerProfileId == farmerProfileId)
            .ToListAsync();

    public async Task AddAsync(FarmerDocument farmerDocument)
    {
        await context.FarmerDocuments.AddAsync(farmerDocument);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FarmerDocument farmerDocument)
    {
        context.FarmerDocuments.Update(farmerDocument);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(FarmerDocument farmerDocument)
    {
        context.FarmerDocuments.Remove(farmerDocument);
        await context.SaveChangesAsync();
    }
}
