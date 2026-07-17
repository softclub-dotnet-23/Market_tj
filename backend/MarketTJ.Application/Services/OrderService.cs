using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.OrderDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class OrderService(
    IOrderRepository orderRepository,
    ICustomerProfileRepository customerProfileRepository,
    IFarmerProfileRepository farmerProfileRepository,
    IUserRepository userRepository,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<Result<IEnumerable<GetOrderDto>>> GetAllAsync()
    {
        try
        {
            var orders = await orderRepository.GetAllAsync();
            return Result<IEnumerable<GetOrderDto>>.Ok(orders.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка заказов");
            return Result<IEnumerable<GetOrderDto>>.Fail("Не удалось получить список заказов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetOrderDto?>> GetByIdAsync(int id)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(id);
            if (order is null)
                return Result<GetOrderDto?>.Fail("Заказ не найден", ErrorType.NotFound);

            return Result<GetOrderDto?>.Ok(ToGetDto(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении заказа {Id}", id);
            return Result<GetOrderDto?>.Fail("Не удалось получить заказ", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateOrderDto dto)
    {
        try
        {
            var validation = OrderValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var customerProfile = await customerProfileRepository.GetByIdAsync(dto.CustomerId);
            if (customerProfile is null)
                return Result<string>.Fail("Профиль покупателя не найден", ErrorType.NotFound);

            // Раздел 21 ТЗ (Order): Customer активен.
            var customerUser = await userRepository.GetByIdAsync(customerProfile.UserId);
            if (customerUser is null || !customerUser.IsActive)
                return Result<string>.Fail("Покупатель неактивен", ErrorType.Validation);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            // Раздел 21 ТЗ (Order): Farmer подтверждён.
            if (farmerProfile.VerificationStatus != FarmerVerificationStatus.Verified)
                return Result<string>.Fail("Фермер не подтверждён", ErrorType.Validation);

            var all = await orderRepository.GetAllAsync();
            if (all.Any(o => o.OrderNumber == dto.OrderNumber))
                return Result<string>.Fail("Заказ с таким номером уже существует", ErrorType.Conflict);

            // Раздел 10.4 ТЗ: после создания заказ получает статус Pending;
            // стоимость заказа считается на сервере, клиент не должен
            // передавать итоговую стоимость вручную. Полный пересчёт от
            // состава корзины здесь невозможен — CreateOrderDto не содержит
            // позиций заказа (это отдельная сущность OrderItem/сервис) —
            // пересчитываем то, что можем проверить на этом уровне.
            var order = new Order
            {
                OrderNumber = dto.OrderNumber,
                CustomerId = dto.CustomerId,
                FarmerId = dto.FarmerId,
                Status = OrderStatus.Pending,
                DeliveryAddress = dto.DeliveryAddress,
                Region = dto.Region,
                District = dto.District,
                CustomerComment = dto.CustomerComment,
                Subtotal = dto.Subtotal,
                DeliveryPrice = dto.DeliveryPrice,
                TotalAmount = dto.Subtotal + dto.DeliveryPrice,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await orderRepository.AddAsync(order);
            return Result<string>.Ok("Заказ создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании заказа");
            return Result<string>.Fail("Не удалось создать заказ", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateOrderDto dto)
    {
        try
        {
            var validation = OrderValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(id);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 10.4 ТЗ: завершённый заказ нельзя редактировать.
            if (order.Status == OrderStatus.Completed)
                return Result<string>.Fail("Завершённый заказ нельзя редактировать", ErrorType.Validation);

            var customerProfile = await customerProfileRepository.GetByIdAsync(dto.CustomerId);
            if (customerProfile is null)
                return Result<string>.Fail("Профиль покупателя не найден", ErrorType.NotFound);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            var all = await orderRepository.GetAllAsync();
            if (all.Any(o => o.Id != id && o.OrderNumber == dto.OrderNumber))
                return Result<string>.Fail("Заказ с таким номером уже существует", ErrorType.Conflict);

            order.OrderNumber = dto.OrderNumber;
            order.CustomerId = dto.CustomerId;
            order.FarmerId = dto.FarmerId;
            order.Status = dto.Status;
            order.DeliveryAddress = dto.DeliveryAddress;
            order.Region = dto.Region;
            order.District = dto.District;
            order.CustomerComment = dto.CustomerComment;
            order.Subtotal = dto.Subtotal;
            order.DeliveryPrice = dto.DeliveryPrice;
            order.TotalAmount = dto.Subtotal + dto.DeliveryPrice;
            order.AcceptedAt = dto.Status == OrderStatus.Accepted && order.AcceptedAt is null ? DateTime.UtcNow : dto.AcceptedAt;
            order.CompletedAt = dto.Status == OrderStatus.Completed && order.CompletedAt is null ? DateTime.UtcNow : dto.CompletedAt;
            order.CancelledAt = dto.Status == OrderStatus.Cancelled && order.CancelledAt is null ? DateTime.UtcNow : dto.CancelledAt;

            await orderRepository.UpdateAsync(order);
            return Result<string>.Ok("Заказ обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении заказа {Id}", id);
            return Result<string>.Fail("Не удалось обновить заказ", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(id);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 18 ТЗ: soft delete (у Order есть IsDeleted/DeletedAt).
            order.IsDeleted = true;
            order.DeletedAt = DateTime.UtcNow;

            await orderRepository.UpdateAsync(order);
            return Result<string>.Ok("Заказ удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении заказа {Id}", id);
            return Result<string>.Fail("Не удалось удалить заказ", ErrorType.InternalServerError);
        }
    }

    private static GetOrderDto ToGetDto(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        CustomerId = order.CustomerId,
        FarmerId = order.FarmerId,
        Status = order.Status,
        DeliveryAddress = order.DeliveryAddress,
        Region = order.Region,
        District = order.District,
        CustomerComment = order.CustomerComment,
        Subtotal = order.Subtotal,
        DeliveryPrice = order.DeliveryPrice,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        AcceptedAt = order.AcceptedAt,
        CompletedAt = order.CompletedAt,
        CancelledAt = order.CancelledAt
    };
}
