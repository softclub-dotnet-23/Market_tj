using MarketTJ.Application.Common;
using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ReportedListingDto;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Interfaces.Services;

public interface IReportedListingService
{
    Task<Result<IEnumerable<GetReportedListingDto>>> GetAllAsync();
    Task<Result<GetReportedListingDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateReportedListingDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateReportedListingDto dto);
    Task<Result<string>> DeleteAsync(int id);

    Task<Result<PagedResult<GetReportedListingDto>>> GetPagedAsync(PagedRequest request, ReportStatus? status);
    Task<Result<string>> ResolveAsync(int id, ReportStatus resolution, int adminId);
}
