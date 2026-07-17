using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.UserDto;

public class CreateUserDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}
