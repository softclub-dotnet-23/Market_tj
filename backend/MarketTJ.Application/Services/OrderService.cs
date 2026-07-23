using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuditLogDto;
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
    IAuditLogService auditLogService,
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

    public async Task<Result<PagedResult<GetOrderDto>>> GetPagedAsync(PagedRequest request, OrderStatus? status)
    {
        try
        {
            var all = await orderRepository.GetAllAsync();

            IEnumerable<Order> filtered = all;
            if (status is not null)
                filtered = filtered.Where(o => o.Status == status);

            filtered = Sort(filtered, request.SortBy, request.SortDescending);

            var materialized = filtered.ToList();
            var page = materialized
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(ToGetDto)
                .ToList();

            return Result<PagedResult<GetOrderDto>>.Ok(
                PagedResult<GetOrderDto>.Ok(page, materialized.Count, request.PageNumber, request.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка заказов (paged)");
            return Result<PagedResult<GetOrderDto>>.Fail("Не удалось получить список заказов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> ChangeStatusAsync(int id, OrderStatus status, int adminId)
    {
        try
        {
            if (!Enum.IsDefined(status))
                return Result<string>.Fail("Указан несуществующий статус заказа", ErrorType.Validation);

            var order = await orderRepository.GetByIdAsync(id);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 10.4 ТЗ: завершённый заказ нельзя редактировать.
            if (order.Status == OrderStatus.Completed)
                return Result<string>.Fail("Завершённый заказ нельзя редактировать", ErrorType.Validation);

            if (order.Status == status)
                return Result<string>.Ok("У заказа уже этот статус");

            var previousStatus = order.Status;
            order.Status = status;
            order.AcceptedAt = status == OrderStatus.Accepted && order.AcceptedAt is null ? DateTime.UtcNow : order.AcceptedAt;
            order.CompletedAt = status == OrderStatus.Completed && order.CompletedAt is null ? DateTime.UtcNow : order.CompletedAt;
            order.CancelledAt = status == OrderStatus.Cancelled && order.CancelledAt is null ? DateTime.UtcNow : order.CancelledAt;

            await orderRepository.UpdateAsync(order);

            await auditLogService.CreateAsync(new CreateAuditLogDto
            {
                AdminId = adminId,
                Action = "ChangeOrderStatus",
                EntityType = nameof(Order),
                EntityId = id,
                Details = $"Статус изменён с {previousStatus} на {status}"
            });

            return Result<string>.Ok("Статус заказа изменён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при изменении статуса заказа {Id}", id);
            return Result<string>.Fail("Не удалось изменить статус заказа", ErrorType.InternalServerError);
        }
    }

    private static IEnumerable<Order> Sort(IEnumerable<Order> orders, string? sortBy, bool descending)
    {
        Func<Order, object> keySelector = sortBy?.ToLowerInvariant() switch
        {
            "totalamount" => o => o.TotalAmount,
            "status" => o => o.Status,
            "ordernumber" => o => o.OrderNumber,
            _ => o => o.CreatedAt
        };

        return descending ? orders.OrderByDescending(keySelector) : orders.OrderBy(keySelector);
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
