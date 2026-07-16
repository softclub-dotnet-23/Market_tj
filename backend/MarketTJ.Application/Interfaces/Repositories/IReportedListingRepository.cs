using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IReportedListingRepository
{
    Task<List<ReportedListing>> GetAllAsync();
    Task<ReportedListing?> GetByIdAsync(int id);
    Task AddAsync(ReportedListing reportedListing);
    Task UpdateAsync(ReportedListing reportedListing);
    Task DeleteAsync(ReportedListing reportedListing);
}
