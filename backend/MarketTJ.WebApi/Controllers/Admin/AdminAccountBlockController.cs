using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

// Единая страница "Заблокированные аккаунты" (Блок 2 + Блок 3, 2026-08-08) —
// показывает баны за отмены заказов (BlockType=Cancellations) и, позже,
// технические rate-limit баны (BlockType=RateLimit) — один и тот же список и
// одна и та же кнопка ручной разблокировки для обоих случаев.
[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin/account-blocks")]
public class AdminAccountBlockController(IAccountBlockService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, [FromQuery] bool? activeOnly)
        => HandleResult(await service.GetAllAsync(request, activeOnly));

    [HttpPost("{id:int}/unblock")]
    public async Task<IActionResult> Unblock(int id)
        => HandleResult(await service.UnblockAsync(id));
}
