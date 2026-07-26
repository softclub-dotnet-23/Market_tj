using MarketTJ.Application.Interfaces.Services;
using MarketTJ.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Самообслуживание для ЛЮБОЙ роли (Admin/Farmer/Customer/Courier) — не под
// UserController, т.к. тот целиком [Authorize(Roles = "Admin")], а множественные
// [Authorize] на классе и методе в ASP.NET Core комбинируются через AND, а не
// перекрывают друг друга, так что открыть один метод для всех ролей оттуда
// нельзя. UserId берётся из JWT-claims (раздел 16 ТЗ — не принимать UserId от
// клиента), поэтому в маршруте нет {id}.
[Authorize]
[Route("api/me")]
public class MeController(IUserService service, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarRequest request)
        => HandleResult(await service.UploadAvatarAsync(currentUser.UserId!.Value, request.File.OpenReadStream(), request.File.FileName, request.File.Length));

    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteAvatar()
        => HandleResult(await service.DeleteAvatarAsync(currentUser.UserId!.Value));
}
