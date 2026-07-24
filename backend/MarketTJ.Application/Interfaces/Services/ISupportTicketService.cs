using MarketTJ.Application.Common;
using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.SupportTicketDto;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Interfaces.Services;

public interface ISupportTicketService
{
    Task<Result<IEnumerable<GetSupportTicketDto>>> GetAllAsync();
    Task<Result<GetSupportTicketDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateSupportTicketDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateSupportTicketDto dto);
    Task<Result<string>> DeleteAsync(int id);

    Task<Result<PagedResult<GetSupportTicketDto>>> GetPagedAsync(PagedRequest request, SupportTicketStatus? status);
}
