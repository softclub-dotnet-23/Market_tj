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
public class AuthController(IAuthService service, IEmailVerificationService emailVerificationService) : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        => HandleResult(await service.RegisterAsync(dto));

    // Раздел 23 ТЗ дополнен по явному запросу пользователя: подтверждение
    // email кодом перед регистрацией (раньше регистрация была одношаговой).
    // Проверка "email уже занят" — через service (та же логика, что и в
    // RegisterAsync), чтобы конфликт всплывал сразу, до отправки кода.
    [HttpPost("send-verification-code")]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeRequestDto dto)
        => HandleResult(await service.SendRegistrationVerificationCodeAsync(dto.Email));

    [HttpPost("verify-email-code")]
    public async Task<IActionResult> VerifyEmailCode([FromBody] VerifyEmailCodeRequestDto dto)
        => HandleResult(await emailVerificationService.VerifyCodeAsync(dto.Email, dto.Code));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        => HandleResult(await service.LoginAsync(dto));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        => HandleResult(await service.RefreshTokenAsync(dto));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        => HandleResult(await service.LogoutAsync(dto));

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        => HandleResult(await service.ForgotPasswordAsync(dto));

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        => HandleResult(await service.ResetPasswordAsync(dto));
}
