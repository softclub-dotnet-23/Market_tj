using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Route("api/ai-assistant")]
public class AiAssistantController(IAiAssistantService aiAssistantService) : ApiControllerBase
{
    // Контроллер не содержит бизнес-логику (раздел 19 ТЗ) — только вызов
    // сервиса и приведение Result<T> к единому формату ответа (раздел 20).
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskAssistantDto dto)
        => HandleResult(await aiAssistantService.AskAsync(dto.Message));
}
