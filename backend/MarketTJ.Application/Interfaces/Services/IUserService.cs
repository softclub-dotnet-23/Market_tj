using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.UserDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IUserService
{
    Task<Result<IEnumerable<GetUserDto>>> GetAllAsync();
    Task<Result<GetUserDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateUserDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateUserDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
