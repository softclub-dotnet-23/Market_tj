using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

// Только чтение (раздел 8.19 ТЗ) — Update/Delete намеренно не выставлены,
// хотя IAuditLogService их технически поддерживает (нужны для generic CRUD
// на /api/audit-logs, куда пишет сам AuditLogService.CreateAsync).
[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin/audit-logs")]
public class AdminAuditLogController(IAuditLogService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedRequest request,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? adminId,
        [FromQuery] string? action)
        => HandleResult(await service.GetPagedAsync(request, from, to, adminId, action));
}
