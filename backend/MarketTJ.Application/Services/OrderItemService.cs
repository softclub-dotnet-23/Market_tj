using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.OrderItemDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class OrderItemService(
    IOrderItemRepository orderItemRepository,
    IOrderRepository orderRepository,
    IProductListingRepository productListingRepository,
    ILogger<OrderItemService> logger) : IOrderItemService
{
    public async Task<Result<IEnumerable<GetOrderItemDto>>> GetAllAsync()
    {
        try
        {
            var items = await orderItemRepository.GetAllAsync();
            return Result<IEnumerable<GetOrderItemDto>>.Ok(items.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении позиций заказов");
            return Result<IEnumerable<GetOrderItemDto>>.Fail("Не удалось получить позиции заказов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetOrderItemDto?>> GetByIdAsync(int id)
    {
        try
        {
            var item = await orderItemRepository.GetByIdAsync(id);
            if (item is null)
                return Result<GetOrderItemDto?>.Fail("Позиция заказа не найдена", ErrorType.NotFound);

            return Result<GetOrderItemDto?>.Ok(ToGetDto(item));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении позиции заказа {Id}", id);
            return Result<GetOrderItemDto?>.Fail("Не удалось получить позицию заказа", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateOrderItemDto dto)
    {
        try
        {
            var validation = OrderItemValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (order.Status == OrderStatus.Completed)
                return Result<string>.Fail("Завершённый заказ нельзя редактировать", ErrorType.Validation);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            var item = new OrderItem
            {
                OrderId = dto.OrderId,
                ProductListingId = dto.ProductListingId,
                ProductName = dto.ProductName,
                UnitPrice = dto.UnitPrice,
                Quantity = dto.Quantity,
                // Раздел 8.11 ТЗ: TotalPrice = UnitPrice × Quantity — считается
                // на сервере, не принимается от клиента.
                TotalPrice = dto.UnitPrice * dto.Quantity,
                CreatedAt = DateTime.UtcNow
            };

            await orderItemRepository.AddAsync(item);
            return Result<string>.Ok("Позиция заказа создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании позиции заказа");
            return Result<string>.Fail("Не удалось создать позицию заказа", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateOrderItemDto dto)
    {
        try
        {
            var validation = OrderItemValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var item = await orderItemRepository.GetByIdAsync(id);
            if (item is null)
                return Result<string>.Fail("Позиция заказа не найдена", ErrorType.NotFound);

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (order.Status == OrderStatus.Completed)
                return Result<string>.Fail("Завершённый заказ нельзя редактировать", ErrorType.Validation);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            item.OrderId = dto.OrderId;
            item.ProductListingId = dto.ProductListingId;
            item.ProductName = dto.ProductName;
            item.UnitPrice = dto.UnitPrice;
            item.Quantity = dto.Quantity;
            item.TotalPrice = dto.UnitPrice * dto.Quantity;

            await orderItemRepository.UpdateAsync(item);
            return Result<string>.Ok("Позиция заказа обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении позиции заказа {Id}", id);
            return Result<string>.Fail("Не удалось обновить позицию заказа", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var item = await orderItemRepository.GetByIdAsync(id);
            if (item is null)
                return Result<string>.Fail("Позиция заказа не найдена", ErrorType.NotFound);

            await orderItemRepository.DeleteAsync(item);
            return Result<string>.Ok("Позиция заказа удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении позиции заказа {Id}", id);
            return Result<string>.Fail("Не удалось удалить позицию заказа", ErrorType.InternalServerError);
        }
    }

    private static GetOrderItemDto ToGetDto(OrderItem item) => new()
    {
        Id = item.Id,
        OrderId = item.OrderId,
        ProductListingId = item.ProductListingId,
        ProductName = item.ProductName,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
        TotalPrice = item.TotalPrice,
        CreatedAt = item.CreatedAt
    };
}
