namespace MarketTJ.Application.Dto.AuthDto;

public class ResetPasswordRequestDto
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
