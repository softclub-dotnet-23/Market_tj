using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// Раздел 20 ТЗ: "Все endpoint требуют JWT, кроме /api/auth/login и
// /api/auth/register/*" — весь контроллер анонимный (в т.ч. refresh/logout:
// они САМИ являются механизмом аутентификации, требовать Bearer-токен для
// них было бы циклической зависимостью).
[AllowAnonymous]
[Route("api/auth")]
public class AuthController(IAuthService service) : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        => HandleResult(await service.RegisterAsync(dto));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        => HandleResult(await service.LoginAsync(dto));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        => HandleResult(await service.RefreshTokenAsync(dto));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        => HandleResult(await service.LogoutAsync(dto));
}
