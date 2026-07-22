using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.Admin;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Tags("Admin")]
[Route("api/admin/users")]
public class AdminUserController(IUserService service, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, [FromQuery] UserRole? role, [FromQuery] bool? isActive)
        => HandleResult(await service.GetPagedAsync(request, role, isActive));

    [HttpPatch("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
        => HandleResult(await service.ActivateAsync(id, currentUser.UserId!.Value));

    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
        => HandleResult(await service.DeactivateAsync(id, currentUser.UserId!.Value));

    [HttpPatch("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeUserRoleDto dto)
        => HandleResult(await service.ChangeRoleAsync(id, dto.Role, currentUser.UserId!.Value));
}
