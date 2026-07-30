using MarketTJ.Application.Dto.AuthDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<Result<string>> LogoutAsync(RefreshTokenRequestDto dto);

    // Дополнено по явному запросу пользователя — раньше "Email уже
    // зарегистрирован" всплывало только на самом последнем шаге регистрации
    // (после ввода кода и заполнения всех данных). Теперь эта же проверка,
    // что и в RegisterAsync, идёт ДО отправки кода на email — пользователь
    // узнаёт о конфликте сразу на первом шаге, а не после лишнего письма.
    Task<Result<string>> SendRegistrationVerificationCodeAsync(string email);

    // Дополнено по явному запросу пользователя — раньше "Забыли пароль?" была
    // честной заглушкой без реализации. Переиспользует тот же механизм кода
    // на email, что и подтверждение при регистрации (IEmailVerificationService).
    Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequestDto dto);
    Task<Result<string>> ResetPasswordAsync(ResetPasswordRequestDto dto);
}
