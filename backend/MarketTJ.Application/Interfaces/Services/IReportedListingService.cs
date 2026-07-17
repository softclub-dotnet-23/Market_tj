using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ReportedListingDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IReportedListingService
{
    Task<Result<IEnumerable<GetReportedListingDto>>> GetAllAsync();
    Task<Result<GetReportedListingDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateReportedListingDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateReportedListingDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
