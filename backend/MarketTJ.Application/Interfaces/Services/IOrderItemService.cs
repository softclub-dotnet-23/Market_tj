using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.OrderItemDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IOrderItemService
{
    Task<Result<IEnumerable<GetOrderItemDto>>> GetAllAsync();
    Task<Result<GetOrderItemDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateOrderItemDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateOrderItemDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
