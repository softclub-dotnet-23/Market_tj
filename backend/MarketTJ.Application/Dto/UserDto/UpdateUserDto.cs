using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.UserDto;

public class UpdateUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    // Необязательное — null/пусто означает "не менять пароль". Если Admin
    // хочет сменить пароль пользователю, передаёт новый raw-пароль сюда;
    // он хэшируется в UserService.UpdateAsync тем же BCrypt, что и везде.
    public string? Password { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}
