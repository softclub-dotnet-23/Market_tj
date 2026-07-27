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
    IOrderItemRepository orderItemRepository,
    IProductListingRepository productListingRepository,
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
            var customers = await ResolveCustomerContactsAsync(orders.Select(o => o.CustomerId));
            return Result<IEnumerable<GetOrderDto>>.Ok(orders.Select(o => ToGetDto(o, customers)));
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

            var customers = await ResolveCustomerContactsAsync([order.CustomerId]);
            return Result<GetOrderDto?>.Ok(ToGetDto(order, customers));
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

            // Раздел 10.4 ТЗ: остаток по объявлениям списывается сразу при
            // добавлении позиции заказа (OrderItemService.CreateAsync) — если
            // заказ отклоняют/отменяют впервые, списанное нужно вернуть назад,
            // иначе товар "теряется" из остатка навсегда без реальной продажи.
            var previousStatus = order.Status;
            var becameRejectedOrCancelled =
                previousStatus != OrderStatus.Rejected && previousStatus != OrderStatus.Cancelled &&
                (dto.Status == OrderStatus.Rejected || dto.Status == OrderStatus.Cancelled);

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

            if (becameRejectedOrCancelled)
                await RestoreStockForOrderAsync(id);

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
            var pageOrders = materialized
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
            var customers = await ResolveCustomerContactsAsync(pageOrders.Select(o => o.CustomerId));
            var page = pageOrders.Select(o => ToGetDto(o, customers)).ToList();

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

    private async Task RestoreStockForOrderAsync(int orderId)
    {
        var items = await orderItemRepository.GetAllAsync();
        foreach (var item in items.Where(i => i.OrderId == orderId))
        {
            var listing = await productListingRepository.GetByIdAsync(item.ProductListingId);
            if (listing is null) continue;

            listing.AvailableQuantity += item.Quantity;
            await productListingRepository.UpdateAsync(listing);
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

    // Order.CustomerId — это CustomerProfile.Id, а имя/телефон лежат в User
    // (раздел 9 ТЗ: связь через профиль, не напрямую). /api/users доступен
    // только Admin (UserController), поэтому резолвим здесь, на сервере, и
    // отдаём уже готовую строку — так у Farmer/Admin один и тот же честный
    // источник данных, без похода на закрытый эндпоинт.
    private async Task<Dictionary<int, (string? FullName, string? Phone)>> ResolveCustomerContactsAsync(IEnumerable<int> customerProfileIds)
    {
        var neededIds = customerProfileIds.Distinct().ToHashSet();
        var profiles = await customerProfileRepository.GetAllAsync();
        var relevantProfiles = profiles.Where(p => neededIds.Contains(p.Id)).ToList();

        var neededUserIds = relevantProfiles.Select(p => p.UserId).Distinct().ToHashSet();
        var users = await userRepository.GetAllAsync();
        var usersById = users.Where(u => neededUserIds.Contains(u.Id)).ToDictionary(u => u.Id);

        var result = new Dictionary<int, (string?, string?)>();
        foreach (var profile in relevantProfiles)
        {
            if (usersById.TryGetValue(profile.UserId, out var user))
                result[profile.Id] = (user.FullName, user.PhoneNumber);
        }
        return result;
    }

    private static GetOrderDto ToGetDto(Order order, IReadOnlyDictionary<int, (string? FullName, string? Phone)> customers)
    {
        customers.TryGetValue(order.CustomerId, out var customer);
        return new()
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
            CancelledAt = order.CancelledAt,
            CustomerFullName = customer.FullName,
            CustomerPhone = customer.Phone
        };
    }
}
