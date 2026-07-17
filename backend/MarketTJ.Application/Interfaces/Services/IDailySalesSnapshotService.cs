using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.DailySalesSnapshotDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IDailySalesSnapshotService
{
    Task<Result<IEnumerable<GetDailySalesSnapshotDto>>> GetAllAsync();
    Task<Result<GetDailySalesSnapshotDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateDailySalesSnapshotDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateDailySalesSnapshotDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
