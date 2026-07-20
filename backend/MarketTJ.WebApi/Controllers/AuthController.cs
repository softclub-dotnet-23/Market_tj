using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

// TODO: временный логин без токена (см. AuthService) — заменить на выдачу
// JWT, когда появится полноценная аутентификация (Этап 2, раздел 23 ТЗ).
[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        => HandleResult(await authService.LoginAsync(dto));
}
