using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

// Только чтение — журнал пишет сам AiAssistantService после каждого
// вопроса-ответа, здесь только просмотр админом (см. Блок 1.4, 2026-08-08).
[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin/ai-conversation-logs")]
public class AdminAiConversationLogController(IAiConversationLogService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, [FromQuery] bool? wasError)
        => HandleResult(await service.GetPagedAsync(request, wasError));
}
