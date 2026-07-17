using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.CartItemDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ICartItemService
{
    Task<Result<IEnumerable<GetCartItemDto>>> GetAllAsync();
    Task<Result<GetCartItemDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateCartItemDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateCartItemDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
