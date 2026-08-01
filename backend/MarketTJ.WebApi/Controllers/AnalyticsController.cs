using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Authorize]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService, ICurrentUserService currentUser) : ApiControllerBase
{
    // Контроллер не содержит бизнес-логику (раздел 19 ТЗ) — только вызов
    // сервиса и приведение Result<T> к единому формату ответа (раздел 20).
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/dashboard")]
    public async Task<IActionResult> GetAdminDashboard()
        => HandleResult(await analyticsService.GetAdminDashboardAsync());

    // Раздел 16 ТЗ: farmerId больше не принимается как query-параметр (клиент
    // мог бы подставить чужой) — UserId берётся из JWT-claims авторизованного
    // фермера, профиль резолвится внутри AnalyticsService.
    [Authorize(Roles = "Farmer")]
    [HttpGet("farmer/dashboard")]
    public async Task<IActionResult> GetFarmerDashboard()
        => HandleResult(await analyticsService.GetFarmerDashboardAsync(currentUser.UserId!.Value));

    // Карточка фермера в админке: та же аналитика, но по конкретному профилю.
    // Здесь id в маршруте безопасен — доступ ограничен ролью Admin.
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/farmers/{farmerProfileId:int}/dashboard")]
    public async Task<IActionResult> GetFarmerDashboardForAdmin(int farmerProfileId)
        => HandleResult(await analyticsService.GetFarmerDashboardByProfileIdAsync(farmerProfileId));
}
