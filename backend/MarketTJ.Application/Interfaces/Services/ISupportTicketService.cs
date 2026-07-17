using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.SupportTicketDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ISupportTicketService
{
    Task<Result<IEnumerable<GetSupportTicketDto>>> GetAllAsync();
    Task<Result<GetSupportTicketDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateSupportTicketDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateSupportTicketDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
