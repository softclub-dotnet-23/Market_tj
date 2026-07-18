using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    // Контроллер не содержит бизнес-логику (раздел 19 ТЗ) — только вызов
    // сервиса и приведение Result<T> к единому формату ответа (раздел 20).
    //
    // TODO: в проекте пока нет системы аутентификации (JWT/Identity —
    // "Этап 2", раздел 23 ТЗ), поэтому [Authorize(Roles = "Admin")] здесь не
    // применён. Когда Auth появится — добавить [Authorize(Roles = "Admin")]
    // на этот метод.
    [HttpGet("admin/dashboard")]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var result = await analyticsService.GetAdminDashboardAsync();
        return ToActionResult(result);
    }

    // TODO: в проекте пока нет системы аутентификации — FarmerId временно
    // принимается как query-параметр вместо чтения из claims текущего
    // пользователя. Когда появится Auth — добавить [Authorize(Roles =
    // "Farmer")] и брать farmerId из claims, чтобы фермер не мог запросить
    // чужие данные через подмену параметра.
    [HttpGet("farmer/dashboard")]
    public async Task<IActionResult> GetFarmerDashboard([FromQuery] int farmerId)
    {
        var result = await analyticsService.GetFarmerDashboardAsync(farmerId);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Application.Results.Result<T> result)
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
