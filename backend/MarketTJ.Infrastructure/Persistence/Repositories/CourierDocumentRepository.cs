using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class CourierDocumentRepository(AppDbContext context) : ICourierDocumentRepository
{
    public async Task<List<CourierDocument>> GetAllAsync()
        => await context.CourierDocuments.AsNoTracking().ToListAsync();

    public async Task<CourierDocument?> GetByIdAsync(int id)
        => await context.CourierDocuments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<List<CourierDocument>> GetByCourierProfileIdAsync(int courierProfileId)
        => await context.CourierDocuments
            .AsNoTracking()
            .Where(x => x.CourierProfileId == courierProfileId)
            .ToListAsync();

    public async Task AddAsync(CourierDocument courierDocument)
    {
        await context.CourierDocuments.AddAsync(courierDocument);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CourierDocument courierDocument)
    {
        context.CourierDocuments.Update(courierDocument);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(CourierDocument courierDocument)
    {
        context.CourierDocuments.Remove(courierDocument);
        await context.SaveChangesAsync();
    }
}
