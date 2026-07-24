using MarketTJ.Application.Common;
using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.UserDto;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<IEnumerable<GetUserDto>>> GetAllAsync();
    Task<Result<GetUserDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateUserDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateUserDto dto);
    Task<Result<string>> DeleteAsync(int id);

    // Admin-специфичные действия (не CRUD) — добавлены поверх существующих
    // методов, сигнатуры выше не менялись.
    Task<Result<PagedResult<GetUserDto>>> GetPagedAsync(PagedRequest request, UserRole? role, bool? isActive);
    Task<Result<string>> ActivateAsync(int id, int adminId);
    Task<Result<string>> DeactivateAsync(int id, int adminId);
    Task<Result<string>> ChangeRoleAsync(int id, UserRole role, int adminId);
}
