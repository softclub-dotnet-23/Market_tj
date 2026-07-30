namespace MarketTJ.Application.Dto.AuthDto;

public class VerifyEmailCodeRequestDto
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
}
