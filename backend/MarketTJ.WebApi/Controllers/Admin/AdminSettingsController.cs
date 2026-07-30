using MarketTJ.Application.Dto.PlatformSettingsDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin/settings")]
public class AdminSettingsController(IPlatformSettingsService service, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
        => HandleResult(await service.GetAsync());

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePlatformSettingsDto dto)
        => HandleResult(await service.UpdateAsync(dto, currentUser.UserId!.Value));
}
