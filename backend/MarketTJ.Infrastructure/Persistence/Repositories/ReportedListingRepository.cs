using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class ReportedListingRepository(AppDbContext context) : IReportedListingRepository
{
    public async Task<List<ReportedListing>> GetAllAsync()
        => await context.ReportedListings.ToListAsync();

    public async Task<ReportedListing?> GetByIdAsync(int id)
        => await context.ReportedListings.FindAsync(id);

    public async Task AddAsync(ReportedListing reportedListing)
    {
        await context.ReportedListings.AddAsync(reportedListing);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ReportedListing reportedListing)
    {
        context.ReportedListings.Update(reportedListing);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ReportedListing reportedListing)
    {
        context.ReportedListings.Remove(reportedListing);
        await context.SaveChangesAsync();
    }
}
