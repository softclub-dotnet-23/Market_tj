using MarketTJ.Application.Common;
using MarketTJ.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Общий helper для маппинга Result<T> -> IActionResult (раздел 20 ТЗ), чтобы
// не дублировать эту логику в каждом контроллере — было скопировано вручную
// в AiAssistantController/AnalyticsController, теперь вынесено сюда и оба
// переведены на этот базовый класс.
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
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
