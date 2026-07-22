using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketTJ.WebApi.Controllers;

[Route("api/auth")]
public class AuthController(IAuthService service) : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        => HandleResult(await service.LoginAsync(dto));
}
