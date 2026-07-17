using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.AuditLogDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAuditLogService
{
    Task<Result<IEnumerable<GetAuditLogDto>>> GetAllAsync();
    Task<Result<GetAuditLogDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateAuditLogDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateAuditLogDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
