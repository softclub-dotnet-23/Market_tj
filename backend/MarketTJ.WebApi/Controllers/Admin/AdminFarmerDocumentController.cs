using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin/farmer-documents")]
public class AdminFarmerDocumentController(IFarmerDocumentService service, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, [FromQuery] DocumentReviewStatus? status)
        => HandleResult(await service.GetPagedAsync(request, status));

    [HttpPatch("{id:int}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewFarmerDocumentDto dto)
        => HandleResult(await service.ReviewAsync(id, dto, currentUser.UserId!.Value));
}
