using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ApiControllerBase
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
        => HandleResult(await analyticsService.GetAdminDashboardAsync());

    // TODO: в проекте пока нет системы аутентификации — FarmerId временно
    // принимается как query-параметр вместо чтения из claims текущего
    // пользователя. Когда появится Auth — добавить [Authorize(Roles =
    // "Farmer")] и брать farmerId из claims, чтобы фермер не мог запросить
    // чужие данные через подмену параметра.
    [HttpGet("farmer/dashboard")]
    public async Task<IActionResult> GetFarmerDashboard([FromQuery] int farmerId)
        => HandleResult(await analyticsService.GetFarmerDashboardAsync(farmerId));
}
