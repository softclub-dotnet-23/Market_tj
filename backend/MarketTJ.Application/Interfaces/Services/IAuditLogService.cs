using MarketTJ.Application.Common;
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

    // Раздел 8.19 ТЗ — журнал только для чтения (кроме создания записи самой
    // системой при админ-действиях); "изменений" здесь не бывает, admin-эндпоинт
    // на него намеренно не даёт Update/Delete.
    Task<Result<PagedResult<GetAuditLogDto>>> GetPagedAsync(PagedRequest request, DateTime? from, DateTime? to, int? adminId, string? action);
}
