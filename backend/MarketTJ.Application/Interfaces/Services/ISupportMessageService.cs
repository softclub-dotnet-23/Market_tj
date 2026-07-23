using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.SupportMessageDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ISupportMessageService
{
    Task<Result<IEnumerable<GetSupportMessageDto>>> GetAllAsync();
    Task<Result<GetSupportMessageDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateSupportMessageDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateSupportMessageDto dto);
    Task<Result<string>> DeleteAsync(int id);

    // GetAllAsync() отдаёт сообщения всех тикетов сразу — для страницы одного
    // тикета (GET /api/admin/support-tickets/{id}/messages) нужен список,
    // отфильтрованный по SupportTicketId.
    Task<Result<IEnumerable<GetSupportMessageDto>>> GetByTicketIdAsync(int ticketId);
}
