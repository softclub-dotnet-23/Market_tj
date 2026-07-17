using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.OrderDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IOrderService
{
    Task<Result<IEnumerable<GetOrderDto>>> GetAllAsync();
    Task<Result<GetOrderDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateOrderDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateOrderDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
