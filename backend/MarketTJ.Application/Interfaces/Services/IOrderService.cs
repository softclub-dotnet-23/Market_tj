using MarketTJ.Application.Common;
using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.OrderDto;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Interfaces.Services;

public interface IOrderService
{
    Task<Result<IEnumerable<GetOrderDto>>> GetAllAsync();
    Task<Result<GetOrderDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateOrderDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateOrderDto dto);
    Task<Result<string>> DeleteAsync(int id);

    Task<Result<PagedResult<GetOrderDto>>> GetPagedAsync(PagedRequest request, OrderStatus? status);
    Task<Result<string>> ChangeStatusAsync(int id, OrderStatus status, int adminId);
}
