using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAiConversationLogService
{
    // Пишется самим AiAssistantService после каждого вопроса-ответа (успех
    // и ошибка) — не выставлен ни на один публичный контроллер для записи,
    // только для чтения администратором (см. AdminAiConversationLogController).
    Task LogAsync(int? userId, string role, string question, string response, string intent, bool wasError);

    Task<Result<PagedResult<GetAiConversationLogDto>>> GetPagedAsync(PagedRequest request, bool? wasError);
}
