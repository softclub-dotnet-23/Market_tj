using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[ApiController]
[Route("api/ai-assistant")]
public class AiAssistantController(IAiAssistantService aiAssistantService) : ControllerBase
{
    // Контроллер не содержит бизнес-логику (раздел 19 ТЗ) — только вызов
    // сервиса и приведение Result<T> к единому формату ответа (раздел 20).
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskAssistantDto dto)
    {
        var result = await aiAssistantService.AskAsync(dto.Message);

        if (result.IsSuccess)
        {
            return Ok(new { isSuccess = true, message = "Operation completed successfully", data = result.Data });
        }

        var statusCode = result.ErrorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation or ErrorType.BadRequest => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(statusCode, new
        {
            isSuccess = false,
            message = result.Error,
            errors = new[] { result.Error }
        });
    }
}
