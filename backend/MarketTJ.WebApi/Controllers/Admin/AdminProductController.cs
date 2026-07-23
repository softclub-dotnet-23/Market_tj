using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

// GET /pending + /approve + /reject для ProductListing из ТЗ этой задачи не
// реализованы: у ListingStatus (Draft/Active/OutOfStock/Archived, см.
// Domain/Enums/ListingStatus.cs) нет статуса "на модерации" — переключение
// Draft/Active полностью в руках фермера (см. ProductListingService), модерация
// в этом проекте устроена через жалобы (ReportedListing), а не approve/reject
// самого объявления. Задача сама оговаривала это ("если есть соответствующий
// статус/флаг в сущности") — флага нет, поэтому эндпоинты не выдуманы.
[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin/reported-listings")]
public class AdminProductController(IReportedListingService service, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReportedListings([FromQuery] PagedRequest request, [FromQuery] ReportStatus? status)
        => HandleResult(await service.GetPagedAsync(request, status));

    [HttpPatch("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveReportedListingDto dto)
        => HandleResult(await service.ResolveAsync(id, dto.Status, currentUser.UserId!.Value));
}
