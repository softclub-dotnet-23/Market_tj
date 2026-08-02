using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuditLogDto;
using MarketTJ.Application.Dto.CourierProfileDto;
using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Dto.NotificationDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class DeliveryService(
    IDeliveryRepository deliveryRepository,
    IOrderRepository orderRepository,
    IOrderItemRepository orderItemRepository,
    ICourierProfileRepository courierProfileRepository,
    ICustomerProfileRepository customerProfileRepository,
    IFarmerProfileRepository farmerProfileRepository,
    IUserRepository userRepository,
    INotificationService notificationService,
    IAuditLogService auditLogService,
    ICurrentUserService currentUser,
    ILogger<DeliveryService> logger) : IDeliveryService
{
    // Курьер продвигает доставку строго по одному шагу за раз — с текущего
    // статуса разрешён переход только на указанный следующий (audit
    // 2026-08-02). Delivered достигается отдельно, через ConfirmDeliveryAsync
    // (нужен код от покупателя), поэтому в эту карту не входит.
    private static readonly Dictionary<DeliveryStatus, DeliveryStatus> CourierTransitions = new()
    {
        [DeliveryStatus.Accepted] = DeliveryStatus.GoingToFarmer,
        [DeliveryStatus.GoingToFarmer] = DeliveryStatus.ArrivedAtFarmer,
        [DeliveryStatus.ArrivedAtFarmer] = DeliveryStatus.PickedUp,
        [DeliveryStatus.PickedUp] = DeliveryStatus.InTransit,
        [DeliveryStatus.InTransit] = DeliveryStatus.ArrivedAtClient,
    };

    // Audit 2026-07-28, находка 2.2 (IDOR): владелец — Customer/Farmer заказа
    // (через профили, как в Order) ИЛИ назначенный на доставку Courier (через CourierId).
    private async Task<bool> IsOwnerAsync(Delivery delivery)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;

        var courierProfile = await courierProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        if (courierProfile is not null && delivery.CourierId == courierProfile.Id)
            return true;

        var order = await orderRepository.GetByIdAsync(delivery.OrderId);
        if (order is null)
            return false;

        var customerProfile = await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        if (customerProfile is not null && customerProfile.Id == order.CustomerId)
            return true;

        var farmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return farmerProfile is not null && farmerProfile.Id == order.FarmerId;
    }

    private async Task<bool> IsOrderOwnerAsync(Order order)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;

        var courierProfile = await courierProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        if (courierProfile is not null)
        {
            var delivery = await deliveryRepository.GetByOrderIdAsync(order.Id);
            if (delivery is not null && delivery.CourierId == courierProfile.Id)
                return true;
        }

        var customerProfile = await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        if (customerProfile is not null && customerProfile.Id == order.CustomerId)
            return true;

        var farmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return farmerProfile is not null && farmerProfile.Id == order.FarmerId;
    }

    public async Task<Result<IEnumerable<GetDeliveryDto>>> GetAllAsync()
    {
        try
        {
            var deliveries = await deliveryRepository.GetAllAsync();

            if (!currentUser.IsAdmin())
            {
                var filtered = new List<Delivery>();
                foreach (var delivery in deliveries)
                {
                    if (await IsOwnerAsync(delivery))
                        filtered.Add(delivery);
                }
                deliveries = filtered;
            }

            var dtos = new List<GetDeliveryDto>();
            foreach (var delivery in deliveries)
                dtos.Add(await ToGetDtoAsync(delivery));

            return Result<IEnumerable<GetDeliveryDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка доставок");
            return Result<IEnumerable<GetDeliveryDto>>.Fail("Не удалось получить список доставок", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetDeliveryDto?>> GetByIdAsync(int id)
    {
        try
        {
            var delivery = await deliveryRepository.GetByIdAsync(id);
            if (delivery is null)
                return Result<GetDeliveryDto?>.Fail("Доставка не найдена", ErrorType.NotFound);

            if (!await IsOwnerAsync(delivery))
                return Result<GetDeliveryDto?>.Fail("Нет доступа к этой доставке", ErrorType.Forbidden);

            return Result<GetDeliveryDto?>.Ok(await ToGetDtoAsync(delivery));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении доставки {Id}", id);
            return Result<GetDeliveryDto?>.Fail("Не удалось получить доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateDeliveryDto dto)
    {
        try
        {
            var validation = DeliveryValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // Раздел 8.12 ТЗ: один заказ имеет максимум одну активную доставку.
            var all = await deliveryRepository.GetAllAsync();
            if (all.Any(d => d.OrderId == dto.OrderId))
                return Result<string>.Fail("У этого заказа уже есть доставка", ErrorType.Conflict);

            if (dto.CourierId is not null)
            {
                var courier = await courierProfileRepository.GetByIdAsync(dto.CourierId.Value);
                if (courier is null)
                    return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

                // Раздел 10.5 ТЗ: нельзя назначить курьеру, у которого уже есть
                // активная (не завершённая/не отменённая) доставка одновременно.
                var hasActiveDelivery = all.Any(d => d.CourierId == dto.CourierId
                    && d.Status is not (DeliveryStatus.Delivered or DeliveryStatus.Cancelled));
                if (hasActiveDelivery)
                    return Result<string>.Fail("У курьера уже есть активная доставка — конфликтующее назначение запрещено", ErrorType.Conflict);
            }

            var delivery = new Delivery
            {
                OrderId = dto.OrderId,
                CourierId = dto.CourierId,
                PickupAddress = dto.PickupAddress,
                DeliveryAddress = dto.DeliveryAddress,
                DeliveryPrice = dto.DeliveryPrice,
                Status = dto.Status,
                AssignedAt = dto.CourierId is not null ? dto.AssignedAt ?? DateTime.UtcNow : null,
                PickedUpAt = dto.PickedUpAt,
                DeliveredAt = dto.DeliveredAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await deliveryRepository.AddAsync(delivery);
            return Result<string>.Ok("Доставка создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании доставки");
            return Result<string>.Fail("Не удалось создать доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateDeliveryDto dto)
    {
        try
        {
            var validation = DeliveryValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var delivery = await deliveryRepository.GetByIdAsync(id);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);

            if (!await IsOwnerAsync(delivery))
                return Result<string>.Fail("Нет доступа к этой доставке", ErrorType.Forbidden);

            var order = await orderRepository.GetByIdAsync(dto.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            var all = await deliveryRepository.GetAllAsync();
            if (all.Any(d => d.Id != id && d.OrderId == dto.OrderId))
                return Result<string>.Fail("У этого заказа уже есть доставка", ErrorType.Conflict);

            if (dto.CourierId is not null)
            {
                var courier = await courierProfileRepository.GetByIdAsync(dto.CourierId.Value);
                if (courier is null)
                    return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

                var hasActiveDelivery = all.Any(d => d.Id != id && d.CourierId == dto.CourierId
                    && d.Status is not (DeliveryStatus.Delivered or DeliveryStatus.Cancelled));
                if (hasActiveDelivery)
                    return Result<string>.Fail("У курьера уже есть активная доставка — конфликтующее назначение запрещено", ErrorType.Conflict);
            }

            delivery.OrderId = dto.OrderId;
            delivery.CourierId = dto.CourierId;
            delivery.PickupAddress = dto.PickupAddress;
            delivery.DeliveryAddress = dto.DeliveryAddress;
            delivery.DeliveryPrice = dto.DeliveryPrice;
            delivery.Status = dto.Status;
            delivery.AssignedAt = dto.AssignedAt ?? delivery.AssignedAt;
            delivery.PickedUpAt = dto.PickedUpAt;
            delivery.DeliveredAt = dto.DeliveredAt;
            delivery.UpdatedAt = DateTime.UtcNow;

            await deliveryRepository.UpdateAsync(delivery);
            return Result<string>.Ok("Доставка обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении доставки {Id}", id);
            return Result<string>.Fail("Не удалось обновить доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var delivery = await deliveryRepository.GetByIdAsync(id);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);

            if (!await IsOwnerAsync(delivery))
                return Result<string>.Fail("Нет доступа к этой доставке", ErrorType.Forbidden);

            await deliveryRepository.DeleteAsync(delivery);
            return Result<string>.Ok("Доставка удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении доставки {Id}", id);
            return Result<string>.Fail("Не удалось удалить доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetDeliveryDto?>> GetByOrderIdAsync(int orderId)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(orderId);
            if (order is null)
                return Result<GetDeliveryDto?>.Fail("Заказ не найден", ErrorType.NotFound);

            if (!await IsOrderOwnerAsync(order))
                return Result<GetDeliveryDto?>.Fail("Нет доступа к доставке этого заказа", ErrorType.Forbidden);

            var delivery = await deliveryRepository.GetByOrderIdAsync(orderId);
            return Result<GetDeliveryDto?>.Ok(delivery is null ? null : await ToGetDtoAsync(delivery));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении доставки заказа {OrderId}", orderId);
            return Result<GetDeliveryDto?>.Fail("Не удалось получить доставку заказа", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<GetDeliveryDto>>> GetMyDeliveriesAsync()
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<IEnumerable<GetDeliveryDto>>.Fail("Требуется вход", ErrorType.Forbidden);

            var courierProfile = await courierProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
            if (courierProfile is null)
                return Result<IEnumerable<GetDeliveryDto>>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            var deliveries = await deliveryRepository.GetByCourierIdAsync(courierProfile.Id);
            var dtos = new List<GetDeliveryDto>();
            foreach (var delivery in deliveries)
                dtos.Add(await ToGetDtoAsync(delivery));

            return Result<IEnumerable<GetDeliveryDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении доставок курьера");
            return Result<IEnumerable<GetDeliveryDto>>.Fail("Не удалось получить доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<GetAvailableCourierDto>>> GetAvailableCouriersAsync(AvailableCourierFilter filter)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Result<IEnumerable<GetAvailableCourierDto>>.Fail("Доступно только администратору", ErrorType.Forbidden);

            var couriers = (await courierProfileRepository.GetAllAsync()).Where(c => c.IsActive).ToList();

            if (filter.OnlyAvailable)
                couriers = couriers.Where(c => c.IsAvailable).ToList();
            if (!string.IsNullOrWhiteSpace(filter.Region))
                couriers = couriers.Where(c => c.Region == filter.Region).ToList();
            if (!string.IsNullOrWhiteSpace(filter.TransportType))
                couriers = couriers.Where(c => c.TransportType == filter.TransportType).ToList();
            if (filter.MinRating.HasValue)
                couriers = couriers.Where(c => c.Rating >= filter.MinRating.Value).ToList();

            var courierIds = couriers.Select(c => c.Id).ToList();
            var activeCounts = courierIds.Count > 0
                ? await deliveryRepository.GetActiveCountsByCourierIdsAsync(courierIds)
                : [];
            var completedCounts = courierIds.Count > 0
                ? await deliveryRepository.GetCompletedCountsByCourierIdsAsync(courierIds)
                : [];

            var dtos = new List<GetAvailableCourierDto>();
            foreach (var courier in couriers)
            {
                var user = await userRepository.GetByIdAsync(courier.UserId);
                dtos.Add(new GetAvailableCourierDto
                {
                    Id = courier.Id,
                    UserId = courier.UserId,
                    FullName = user?.FullName ?? "—",
                    PhoneNumber = user?.PhoneNumber ?? "—",
                    AvatarUrl = user?.AvatarUrl,
                    TransportType = courier.TransportType,
                    VehicleNumber = courier.VehicleNumber,
                    Region = courier.Region,
                    District = courier.District,
                    Rating = courier.Rating,
                    IsAvailable = courier.IsAvailable,
                    ActiveDeliveries = activeCounts.GetValueOrDefault(courier.Id, 0),
                    CompletedDeliveries = completedCounts.GetValueOrDefault(courier.Id, 0),
                });
            }

            return Result<IEnumerable<GetAvailableCourierDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка доступных курьеров");
            return Result<IEnumerable<GetAvailableCourierDto>>.Fail("Не удалось получить список курьеров", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> AssignCourierAsync(int orderId, AssignCourierDto dto)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Result<string>.Fail("Назначить курьера может только администратор", ErrorType.Forbidden);

            var order = await orderRepository.GetByIdAsync(orderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (order.Status is OrderStatus.Delivered or OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Rejected)
                return Result<string>.Fail("Нельзя назначить курьера на завершённый или отменённый заказ", ErrorType.Conflict);

            var courier = await courierProfileRepository.GetByIdAsync(dto.CourierId);
            if (courier is null)
                return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);
            if (!courier.IsActive)
                return Result<string>.Fail("Курьер неактивен", ErrorType.Conflict);

            var existing = await deliveryRepository.GetByOrderIdAsync(orderId);

            var otherDeliveries = await deliveryRepository.GetByCourierIdAsync(dto.CourierId);
            var hasConflict = otherDeliveries.Any(d => d.Id != existing?.Id
                && d.Status is not (DeliveryStatus.Delivered or DeliveryStatus.Cancelled));
            if (hasConflict)
                return Result<string>.Fail("У курьера уже есть активная доставка — конфликтующее назначение запрещено", ErrorType.Conflict);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
            var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);

            Delivery delivery;
            if (existing is null)
            {
                delivery = new Delivery
                {
                    OrderId = orderId,
                    CourierId = dto.CourierId,
                    PickupAddress = farmerProfile?.Address ?? "—",
                    DeliveryAddress = order.DeliveryAddress,
                    DeliveryPrice = dto.DeliveryFee,
                    Status = DeliveryStatus.Assigned,
                    AssignedAt = DateTime.UtcNow,
                    EstimatedPickupAt = dto.EstimatedPickupAt,
                    EstimatedDeliveryAt = dto.EstimatedDeliveryAt,
                    AdminNote = dto.AdminNote,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                GenerateConfirmationCode(delivery);
                await deliveryRepository.AddAsync(delivery);
            }
            else
            {
                delivery = existing;
                delivery.CourierId = dto.CourierId;
                delivery.Status = DeliveryStatus.Assigned;
                delivery.AssignedAt = DateTime.UtcNow;
                delivery.AcceptedAt = null;
                delivery.PickedUpAt = null;
                delivery.DeliveredAt = null;
                delivery.DeliveryPrice = dto.DeliveryFee;
                delivery.EstimatedPickupAt = dto.EstimatedPickupAt;
                delivery.EstimatedDeliveryAt = dto.EstimatedDeliveryAt;
                delivery.AdminNote = dto.AdminNote;
                delivery.UpdatedAt = DateTime.UtcNow;
                await deliveryRepository.UpdateAsync(delivery);
            }

            order.Status = OrderStatus.CourierAssigned;
            await orderRepository.UpdateAsync(order);

            if (farmerProfile is not null)
                await NotifyAsync(farmerProfile.UserId, "Курьер назначен", $"На заказ №{order.OrderNumber} назначен курьер.");
            if (customerProfile is not null)
                await NotifyAsync(customerProfile.UserId, "Курьер назначен", $"На ваш заказ №{order.OrderNumber} назначен курьер.");
            await NotifyAsync(courier.UserId, "Новая доставка", $"Вам назначена доставка по заказу №{order.OrderNumber}.");

            await CreateAuditLogAsync("AssignCourier", delivery.Id, $"Курьер #{dto.CourierId} назначен на заказ №{order.OrderNumber}");

            return Result<string>.Ok("Курьер назначен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при назначении курьера на заказ {OrderId}", orderId);
            return Result<string>.Fail("Не удалось назначить курьера", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAdminDetailsAsync(int deliveryId, UpdateDeliveryAdminDetailsDto dto)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Result<string>.Fail("Доступно только администратору", ErrorType.Forbidden);

            var delivery = await deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);

            var estimateChanged = delivery.EstimatedDeliveryAt != dto.EstimatedDeliveryAt;

            delivery.DeliveryPrice = dto.DeliveryFee;
            delivery.EstimatedPickupAt = dto.EstimatedPickupAt;
            delivery.EstimatedDeliveryAt = dto.EstimatedDeliveryAt;
            delivery.AdminNote = dto.AdminNote;
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            if (estimateChanged)
            {
                var order = await orderRepository.GetByIdAsync(delivery.OrderId);
                if (order is not null)
                {
                    var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
                    var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
                    if (farmerProfile is not null)
                        await NotifyAsync(farmerProfile.UserId, "Срок доставки изменён", $"Обновлён ожидаемый срок доставки по заказу №{order.OrderNumber}.");
                    if (customerProfile is not null)
                        await NotifyAsync(customerProfile.UserId, "Срок доставки изменён", $"Обновлён ожидаемый срок доставки по заказу №{order.OrderNumber}.");
                }
            }

            await CreateAuditLogAsync("UpdateDeliveryDetails", delivery.Id, "Изменены сумма/сроки/заметка доставки");

            return Result<string>.Ok("Доставка обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении параметров доставки {Id}", deliveryId);
            return Result<string>.Fail("Не удалось обновить доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CancelAsync(int deliveryId, CancelDeliveryDto dto)
    {
        try
        {
            if (!currentUser.IsAdmin())
                return Result<string>.Fail("Отменить доставку может только администратор", ErrorType.Forbidden);

            var delivery = await deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);

            if (delivery.Status == DeliveryStatus.Delivered)
                return Result<string>.Fail("Нельзя отменить уже доставленный заказ", ErrorType.Conflict);

            delivery.Status = DeliveryStatus.Cancelled;
            delivery.CancelledAt = DateTime.UtcNow;
            delivery.CancellationReason = dto.Reason;
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            var order = await orderRepository.GetByIdAsync(delivery.OrderId);
            if (order is not null && order.Status is OrderStatus.CourierAssigned or OrderStatus.PickedUp or OrderStatus.InDelivery)
            {
                order.Status = OrderStatus.ReadyForPickup;
                await orderRepository.UpdateAsync(order);
            }

            if (order is not null)
            {
                var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
                var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
                if (farmerProfile is not null)
                    await NotifyAsync(farmerProfile.UserId, "Доставка отменена", $"Доставка по заказу №{order.OrderNumber} отменена: {dto.Reason}");
                if (customerProfile is not null)
                    await NotifyAsync(customerProfile.UserId, "Доставка отменена", $"Доставка по вашему заказу №{order.OrderNumber} отменена: {dto.Reason}");
            }
            if (delivery.CourierId is not null)
            {
                var courier = await courierProfileRepository.GetByIdAsync(delivery.CourierId.Value);
                if (courier is not null)
                    await NotifyAsync(courier.UserId, "Доставка отменена", $"Доставка №{delivery.Id} отменена администратором.");
            }

            await CreateAuditLogAsync("CancelDelivery", delivery.Id, $"Доставка отменена: {dto.Reason}");

            return Result<string>.Ok("Доставка отменена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при отмене доставки {Id}", deliveryId);
            return Result<string>.Fail("Не удалось отменить доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> MarkReadyForPickupAsync(int orderId)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(orderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (!currentUser.IsAdmin())
            {
                if (currentUser.UserId is null)
                    return Result<string>.Fail("Требуется вход", ErrorType.Forbidden);
                var farmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (farmerProfile is null || farmerProfile.Id != order.FarmerId)
                    return Result<string>.Fail("Нет доступа к этому заказу", ErrorType.Forbidden);
            }

            var delivery = await deliveryRepository.GetByOrderIdAsync(orderId);
            if (delivery is null)
                return Result<string>.Fail("Сначала администратор должен назначить курьера", ErrorType.Conflict);

            order.Status = OrderStatus.ReadyForPickup;
            await orderRepository.UpdateAsync(order);

            if (delivery.CourierId is not null)
            {
                var courier = await courierProfileRepository.GetByIdAsync(delivery.CourierId.Value);
                if (courier is not null)
                    await NotifyAsync(courier.UserId, "Заказ готов к выдаче", $"Фермер подготовил заказ №{order.OrderNumber} — можно забирать.");
            }

            return Result<string>.Ok("Заказ отмечен как готовый к выдаче");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при отметке заказа {OrderId} готовым к выдаче", orderId);
            return Result<string>.Fail("Не удалось отметить заказ готовым", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> AcceptAsync(int deliveryId)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<string>.Fail("Требуется вход", ErrorType.Forbidden);

            var courierProfile = await courierProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
            if (courierProfile is null)
                return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            var delivery = await deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);
            if (delivery.CourierId != courierProfile.Id)
                return Result<string>.Fail("Эта доставка назначена не вам", ErrorType.Forbidden);
            if (delivery.Status != DeliveryStatus.Assigned)
                return Result<string>.Fail("Принять можно только только что назначенную доставку", ErrorType.Conflict);

            delivery.Status = DeliveryStatus.Accepted;
            delivery.AcceptedAt = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            var order = await orderRepository.GetByIdAsync(delivery.OrderId);
            if (order is not null)
            {
                var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
                if (farmerProfile is not null)
                    await NotifyAsync(farmerProfile.UserId, "Курьер принял доставку", $"Курьер принял доставку по заказу №{order.OrderNumber}.");
            }

            return Result<string>.Ok("Доставка принята");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при принятии доставки {Id}", deliveryId);
            return Result<string>.Fail("Не удалось принять доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateCourierStatusAsync(int deliveryId, CourierStatusUpdateDto dto)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<string>.Fail("Требуется вход", ErrorType.Forbidden);

            var courierProfile = await courierProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
            if (courierProfile is null)
                return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            var delivery = await deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);
            if (delivery.CourierId != courierProfile.Id)
                return Result<string>.Fail("Эта доставка назначена не вам", ErrorType.Forbidden);

            if (!CourierTransitions.TryGetValue(delivery.Status, out var allowedNext) || allowedNext != dto.Status)
                return Result<string>.Fail("Недопустимый переход статуса", ErrorType.Validation);

            delivery.Status = dto.Status;
            if (!string.IsNullOrWhiteSpace(dto.Note))
                delivery.CourierNote = dto.Note;
            if (dto.Status == DeliveryStatus.PickedUp)
                delivery.PickedUpAt = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            var order = await orderRepository.GetByIdAsync(delivery.OrderId);
            if (order is not null)
            {
                if (dto.Status == DeliveryStatus.PickedUp)
                    order.Status = OrderStatus.PickedUp;
                else if (dto.Status == DeliveryStatus.InTransit)
                    order.Status = OrderStatus.InDelivery;
                await orderRepository.UpdateAsync(order);

                var (title, message) = StatusNotificationText(dto.Status, order.OrderNumber);
                var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
                var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
                if (farmerProfile is not null)
                    await NotifyAsync(farmerProfile.UserId, title, message);
                if (customerProfile is not null)
                    await NotifyAsync(customerProfile.UserId, title, message);
            }

            return Result<string>.Ok("Статус доставки обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении статуса доставки {Id}", deliveryId);
            return Result<string>.Fail("Не удалось обновить статус доставки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> ConfirmDeliveryAsync(int deliveryId, ConfirmDeliveryDto dto)
    {
        try
        {
            if (currentUser.UserId is null)
                return Result<string>.Fail("Требуется вход", ErrorType.Forbidden);

            var courierProfile = await courierProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
            if (courierProfile is null)
                return Result<string>.Fail("Профиль курьера не найден", ErrorType.NotFound);

            var delivery = await deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);
            if (delivery.CourierId != courierProfile.Id)
                return Result<string>.Fail("Эта доставка назначена не вам", ErrorType.Forbidden);
            if (delivery.Status != DeliveryStatus.ArrivedAtClient)
                return Result<string>.Fail("Сначала отметьте прибытие к покупателю", ErrorType.Conflict);

            if (delivery.ConfirmationAttempts >= 5)
                return Result<string>.Fail("Превышено число попыток ввода кода — обратитесь к администратору", ErrorType.Validation);

            if (string.IsNullOrEmpty(delivery.ConfirmationCodeHash) || !BCrypt.Net.BCrypt.Verify(dto.Code, delivery.ConfirmationCodeHash))
            {
                delivery.ConfirmationAttempts += 1;
                await deliveryRepository.UpdateAsync(delivery);
                return Result<string>.Fail("Неверный код подтверждения", ErrorType.Validation);
            }

            delivery.Status = DeliveryStatus.Delivered;
            delivery.DeliveredAt = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            var order = await orderRepository.GetByIdAsync(delivery.OrderId);
            if (order is not null)
            {
                order.Status = OrderStatus.Delivered;
                await orderRepository.UpdateAsync(order);

                var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
                var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
                if (farmerProfile is not null)
                    await NotifyAsync(farmerProfile.UserId, "Заказ доставлен", $"Заказ №{order.OrderNumber} успешно доставлен покупателю.");
                if (customerProfile is not null)
                    await NotifyAsync(customerProfile.UserId, "Заказ доставлен", $"Ваш заказ №{order.OrderNumber} доставлен.");
            }

            return Result<string>.Ok("Доставка подтверждена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при подтверждении доставки {Id}", deliveryId);
            return Result<string>.Fail("Не удалось подтвердить доставку", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> ReportProblemAsync(int deliveryId, ReportDeliveryProblemDto dto)
    {
        try
        {
            var delivery = await deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);

            if (!await IsOwnerAsync(delivery))
                return Result<string>.Fail("Нет доступа к этой доставке", ErrorType.Forbidden);

            delivery.ProblemDescription = dto.Description;
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            var admins = (await userRepository.GetAllAsync()).Where(u => u.Role == UserRole.Admin);
            foreach (var admin in admins)
                await NotifyAsync(admin.Id, "Проблема с доставкой", $"Сообщена проблема по доставке №{delivery.Id}: {dto.Description}");

            return Result<string>.Ok("Сообщение о проблеме отправлено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при сообщении о проблеме с доставкой {Id}", deliveryId);
            return Result<string>.Fail("Не удалось отправить сообщение о проблеме", ErrorType.InternalServerError);
        }
    }

    private static void GenerateConfirmationCode(Delivery delivery)
    {
        var code = Random.Shared.Next(0, 10000).ToString("D4");
        delivery.ConfirmationCode = code;
        delivery.ConfirmationCodeHash = BCrypt.Net.BCrypt.HashPassword(code);
        delivery.ConfirmationAttempts = 0;
    }

    private static (string Title, string Message) StatusNotificationText(DeliveryStatus status, string orderNumber) => status switch
    {
        DeliveryStatus.GoingToFarmer => ("Курьер в пути к фермеру", $"Курьер направляется за заказом №{orderNumber}."),
        DeliveryStatus.ArrivedAtFarmer => ("Курьер на месте у фермера", $"Курьер прибыл забрать заказ №{orderNumber}."),
        DeliveryStatus.PickedUp => ("Заказ забран курьером", $"Курьер забрал заказ №{orderNumber}."),
        DeliveryStatus.InTransit => ("Курьер в пути", $"Курьер везёт заказ №{orderNumber} к покупателю."),
        DeliveryStatus.ArrivedAtClient => ("Курьер прибыл", $"Курьер прибыл с заказом №{orderNumber}."),
        _ => ("Статус доставки обновлён", $"Обновлён статус доставки по заказу №{orderNumber}."),
    };

    private async Task NotifyAsync(int userId, string title, string message)
    {
        try
        {
            await notificationService.CreateAsync(new CreateNotificationDto { UserId = userId, Title = title, Message = message, IsRead = false });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось отправить уведомление пользователю {UserId}", userId);
        }
    }

    private async Task CreateAuditLogAsync(string action, int deliveryId, string details)
    {
        if (currentUser.UserId is null)
            return;
        try
        {
            await auditLogService.CreateAsync(new CreateAuditLogDto
            {
                AdminId = currentUser.UserId.Value,
                Action = action,
                EntityType = nameof(Delivery),
                EntityId = deliveryId,
                Details = details,
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось записать AuditLog для доставки {DeliveryId}", deliveryId);
        }
    }

    private async Task<GetDeliveryDto> ToGetDtoAsync(Delivery delivery)
    {
        var dto = new GetDeliveryDto
        {
            Id = delivery.Id,
            OrderId = delivery.OrderId,
            CourierId = delivery.CourierId,
            PickupAddress = delivery.PickupAddress,
            DeliveryAddress = delivery.DeliveryAddress,
            DeliveryPrice = delivery.DeliveryPrice,
            Status = delivery.Status,
            EstimatedPickupAt = delivery.EstimatedPickupAt,
            EstimatedDeliveryAt = delivery.EstimatedDeliveryAt,
            AssignedAt = delivery.AssignedAt,
            AcceptedAt = delivery.AcceptedAt,
            PickedUpAt = delivery.PickedUpAt,
            DeliveredAt = delivery.DeliveredAt,
            CancelledAt = delivery.CancelledAt,
            FarmerNote = delivery.FarmerNote,
            ClientNote = delivery.ClientNote,
            AdminNote = delivery.AdminNote,
            CourierNote = delivery.CourierNote,
            CancellationReason = delivery.CancellationReason,
            ProblemDescription = delivery.ProblemDescription,
            CreatedAt = delivery.CreatedAt,
            UpdatedAt = delivery.UpdatedAt,
        };

        if (delivery.CourierId is not null)
        {
            var courier = await courierProfileRepository.GetByIdAsync(delivery.CourierId.Value);
            if (courier is not null)
            {
                var courierUser = await userRepository.GetByIdAsync(courier.UserId);
                dto.CourierFullName = courierUser?.FullName;
                dto.CourierPhoneNumber = courierUser?.PhoneNumber;
                dto.CourierAvatarUrl = courierUser?.AvatarUrl;
                dto.CourierTransportType = courier.TransportType;
                dto.CourierVehicleNumber = courier.VehicleNumber;
                dto.CourierRating = courier.Rating;
            }
        }

        var order = await orderRepository.GetByIdAsync(delivery.OrderId);
        if (order is not null)
        {
            dto.OrderNumber = order.OrderNumber;
            dto.ItemCount = (await orderItemRepository.GetAllAsync()).Count(i => i.OrderId == order.Id);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
            if (farmerProfile is not null)
            {
                dto.FarmerName = farmerProfile.FarmName;
                var farmerUser = await userRepository.GetByIdAsync(farmerProfile.UserId);
                dto.FarmerPhoneNumber = farmerUser?.PhoneNumber;
            }

            var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
            if (customerProfile is not null)
            {
                var customerUser = await userRepository.GetByIdAsync(customerProfile.UserId);
                dto.CustomerName = customerUser?.FullName;
                dto.CustomerPhoneNumber = customerUser?.PhoneNumber;
            }

            // Код подтверждения видит только покупатель — владелец заказа.
            if (currentUser.UserId is not null && !string.IsNullOrEmpty(delivery.ConfirmationCode) && customerProfile is not null)
            {
                var callerCustomerProfile = await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (callerCustomerProfile is not null && callerCustomerProfile.Id == order.CustomerId)
                    dto.ConfirmationCode = delivery.ConfirmationCode;
            }
        }

        return dto;
    }
}
