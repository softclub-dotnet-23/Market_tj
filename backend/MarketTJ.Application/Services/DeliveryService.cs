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
    IOrderService orderService,
    ICurrentUserService currentUser,
    IGoogleGeocodingService geocodingService,
    IFileStorageService fileStorageService,
    IAccountBlockService accountBlockService,
    ILogger<DeliveryService> logger) : IDeliveryService
{
    // Упрощение находки 2026-08-05: раньше курьер продвигал доставку по 5
    // отдельным шагам (Accepted→GoingToFarmer→ArrivedAtFarmer→PickedUp→
    // InTransit→ArrivedAtClient) — по прямому запросу пользователя ("каждый
    // шаг должен записать, это очень неудобно") сведено к одному действию
    // "Забрал, еду к покупателю" сразу после принятия доставки; подтверждение
    // кодом (ConfirmDeliveryAsync) теперь тоже доступно прямо из InTransit,
    // без обязательной остановки на ArrivedAtClient. GoingToFarmer/
    // ArrivedAtFarmer/PickedUp как источники оставлены здесь только как
    // fallback для доставок, застрявших на этих статусах ДО этого упрощения —
    // у них тоже должна остаться ровно одна кнопка "далее".
    private static readonly Dictionary<DeliveryStatus, DeliveryStatus> CourierTransitions = new()
    {
        [DeliveryStatus.Accepted] = DeliveryStatus.InTransit,
        [DeliveryStatus.GoingToFarmer] = DeliveryStatus.InTransit,
        [DeliveryStatus.ArrivedAtFarmer] = DeliveryStatus.InTransit,
        [DeliveryStatus.PickedUp] = DeliveryStatus.InTransit,
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

    // Строго "фермер — владелец заказа", БЕЗ пропуска для Admin — по прямому
    // запросу пользователя (2026-08-05): назначение курьера (и связанные
    // действия) теперь только фермер, администратор эту роль больше не
    // дублирует ни как основной, ни как запасной путь.
    private async Task<bool> IsFarmerOwnerAsync(Order order)
    {
        if (currentUser.UserId is null)
            return false;

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
            // По прямому запросу пользователя (2026-08-05): смотреть список
            // доступных курьеров и назначать их может ТОЛЬКО фермер — Admin
            // больше не участвует ни как основной, ни как запасной путь (см.
            // тот же запрос на AssignCourierAsync).
            if (currentUser.UserId is null)
                return Result<IEnumerable<GetAvailableCourierDto>>.Fail("Доступно только фермеру", ErrorType.Forbidden);

            var myFarmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
            if (myFarmerProfile is null)
                return Result<IEnumerable<GetAvailableCourierDto>>.Fail("Доступно только фермеру", ErrorType.Forbidden);

            var order = await orderRepository.GetByIdAsync(filter.OrderId);
            if (order is null)
                return Result<IEnumerable<GetAvailableCourierDto>>.Fail("Заказ не найден", ErrorType.NotFound);
            if (order.FarmerId != myFarmerProfile.Id)
                return Result<IEnumerable<GetAvailableCourierDto>>.Fail("Нет доступа к этому заказу", ErrorType.Forbidden);

            // Раздел "выбор курьера по карте в радиусе 40 км" (2026-08-05):
            // координаты адреса доставки геокодируются лениво при первом
            // запросе и кэшируются на Order — повторные запросы для того же
            // заказа не дёргают Google Geocoding API снова.
            if (order.DeliveryLatitude is null || order.DeliveryLongitude is null)
            {
                var geocoded = await geocodingService.GeocodeAsync($"{order.DeliveryAddress}, {order.District}, {order.Region}");
                if (!geocoded.IsSuccess)
                    return Result<IEnumerable<GetAvailableCourierDto>>.Fail(
                        "Не удалось определить координаты адреса доставки — назначьте курьера вручную", ErrorType.Validation);

                order.DeliveryLatitude = geocoded.Data.Latitude;
                order.DeliveryLongitude = geocoded.Data.Longitude;
                await orderRepository.UpdateAsync(order);
            }

            var orderLat = order.DeliveryLatitude.Value;
            var orderLng = order.DeliveryLongitude.Value;

            var couriers = (await courierProfileRepository.GetAllAsync()).Where(c => c.IsActive).ToList();

            // CourierProfile.IsAvailable — это ручной тумблер курьера ("готов
            // брать заказы"), он НЕ отражает, что курьер прямо сейчас уже
            // везёт другую доставку. Раньше бейдж "Свободен"/"Занят" в списке
            // строился только по этому тумблеру, из-за чего курьер с активной
            // доставкой показывался как свободный и назначение падало с
            // "У курьера уже есть активная доставка" только на этапе сохранения.
            // Реальная доступность = тумблер И отсутствие активной Delivery.
            var allCourierIds = couriers.Select(c => c.Id).ToList();
            var activeCounts = allCourierIds.Count > 0
                ? await deliveryRepository.GetActiveCountsByCourierIdsAsync(allCourierIds)
                : [];

            if (filter.OnlyAvailable)
                couriers = couriers.Where(c => c.IsAvailable && activeCounts.GetValueOrDefault(c.Id, 0) == 0).ToList();
            if (!string.IsNullOrWhiteSpace(filter.Region))
                couriers = couriers.Where(c => c.Region == filter.Region).ToList();
            if (!string.IsNullOrWhiteSpace(filter.District))
                couriers = couriers.Where(c => c.District == filter.District).ToList();
            if (!string.IsNullOrWhiteSpace(filter.TransportType))
                couriers = couriers.Where(c => c.TransportType == filter.TransportType).ToList();
            if (filter.MinRating.HasValue)
                couriers = couriers.Where(c => c.Rating >= filter.MinRating.Value).ToList();

            // Блок 2 (2026-08-08) — заблокированный за частые отмены курьер не
            // должен появляться в списке кандидатов для назначения вообще
            // (не просто "нельзя принять" — фермер его даже не видит).
            var blockedUserIds = await accountBlockService.GetActiveBlockedUserIdsAsync(couriers.Select(c => c.UserId));
            if (blockedUserIds.Count > 0)
                couriers = couriers.Where(c => !blockedUserIds.Contains(c.UserId)).ToList();

            // Реальное расстояние (Haversine) от курьера до адреса доставки —
            // заменяет прежнюю сортировку "тот же район первым" (2026-08-05).
            // Курьеры без координат (ещё не геокодированы) исключаются из
            // списка целиком, а не просто опускаются вниз — так же, как и
            // курьеры вне 40-километрового радиуса.
            const double maxDistanceKm = 40.0;
            var withDistance = couriers
                .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                .Select(c => (Courier: c, DistanceKm: GeoDistance.HaversineKm(orderLat, orderLng, c.Latitude!.Value, c.Longitude!.Value)))
                .Where(x => x.DistanceKm <= maxDistanceKm)
                .OrderBy(x => x.DistanceKm)
                .ThenByDescending(x => x.Courier.Rating)
                .ToList();

            var courierIds = withDistance.Select(x => x.Courier.Id).ToList();
            var completedCounts = courierIds.Count > 0
                ? await deliveryRepository.GetCompletedCountsByCourierIdsAsync(courierIds)
                : [];

            var dtos = new List<GetAvailableCourierDto>();
            foreach (var (courier, distanceKm) in withDistance)
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
                    IsAvailable = courier.IsAvailable && activeCounts.GetValueOrDefault(courier.Id, 0) == 0,
                    ActiveDeliveries = activeCounts.GetValueOrDefault(courier.Id, 0),
                    CompletedDeliveries = completedCounts.GetValueOrDefault(courier.Id, 0),
                    DistanceKm = distanceKm,
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
            var order = await orderRepository.GetByIdAsync(orderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            // По прямому запросу пользователя (2026-08-05): назначает курьера
            // ТОЛЬКО фермер — владелец заказа. Раньше (2026-08-04) Admin
            // сохранял это право как запасной вариант — запрос отменён,
            // администратор больше не участвует в приёме заказа/назначении
            // курьера вообще.
            if (!await IsFarmerOwnerAsync(order))
                return Result<string>.Fail("Назначить курьера может только фермер — владелец заказа", ErrorType.Forbidden);

            if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Rejected
                || order.CourierStatus == CourierOrderStatus.Delivered)
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

            // Раньше здесь писали order.Status = OrderStatus.CourierAssigned —
            // по прямому запросу пользователя (2026-08-04) теперь пишем в
            // отдельное FarmerStatus, а не в общий Status (см. комментарий на
            // Order.FarmerStatus/CourierStatus). Status остаётся как есть —
            // источник истины для Pending/Accepted/Rejected/Preparing/
            // Completed/Cancelled и завязанной на них комиссии.
            order.FarmerStatus = FarmerOrderStatus.HandedToCourier;
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

    // По прямому запросу пользователя (2026-08-05) — фермер может назначить
    // курьера "вручную" (знакомого/своего водителя), не выбирая из
    // зарегистрированных на платформе: только имя и телефон, без CourierId.
    // Дальше по флоу такая доставка не отслеживается курьерским приложением
    // (нет аккаунта), поэтому статус подтверждает сам фермер — см.
    // ConfirmManualDeliveryAsync — тем же 4-значным кодом, что видит покупатель.
    public async Task<Result<string>> AssignManualCourierAsync(int orderId, AssignManualCourierDto dto)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(orderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (!await IsFarmerOwnerAsync(order))
                return Result<string>.Fail("Назначить курьера может только фермер — владелец заказа", ErrorType.Forbidden);

            if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Rejected
                || order.CourierStatus == CourierOrderStatus.Delivered)
                return Result<string>.Fail("Нельзя назначить курьера на завершённый или отменённый заказ", ErrorType.Conflict);

            if (string.IsNullOrWhiteSpace(dto.CourierName) || string.IsNullOrWhiteSpace(dto.CourierPhone))
                return Result<string>.Fail("Укажите имя и телефон курьера", ErrorType.Validation);

            var existing = await deliveryRepository.GetByOrderIdAsync(orderId);
            var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
            var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);

            Delivery delivery;
            if (existing is null)
            {
                delivery = new Delivery
                {
                    OrderId = orderId,
                    CourierId = null,
                    ManualCourierName = dto.CourierName.Trim(),
                    ManualCourierPhone = dto.CourierPhone.Trim(),
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
                await deliveryRepository.AddAsync(delivery);
            }
            else
            {
                delivery = existing;
                delivery.CourierId = null;
                delivery.ManualCourierName = dto.CourierName.Trim();
                delivery.ManualCourierPhone = dto.CourierPhone.Trim();
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

            order.FarmerStatus = FarmerOrderStatus.HandedToCourier;
            await orderRepository.UpdateAsync(order);

            if (customerProfile is not null)
                await NotifyAsync(customerProfile.UserId, "Курьер назначен", $"На ваш заказ №{order.OrderNumber} назначен курьер.");

            await CreateAuditLogAsync("AssignManualCourier", delivery.Id, $"Ручной курьер {dto.CourierName} назначен на заказ №{order.OrderNumber}");

            return Result<string>.Ok("Курьер назначен");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при ручном назначении курьера на заказ {OrderId}", orderId);
            return Result<string>.Fail("Не удалось назначить курьера", ErrorType.InternalServerError);
        }
    }

    // Подтверждение доставки для "ручного" курьера — у него нет аккаунта и
    // курьерского приложения, поэтому вместо ConfirmDeliveryAsync (там
    // проверка идёт по CourierProfile текущего пользователя) код вводит сам
    // фермер, получив его от покупателя тем же способом, что и обычный курьер.
    public async Task<Result<string>> ConfirmManualDeliveryAsync(int deliveryId, Stream photoStream, string fileName, long fileLength)
    {
        try
        {
            var delivery = await deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);
            if (delivery.CourierId is not null)
                return Result<string>.Fail("Эта доставка назначена зарегистрированному курьеру", ErrorType.Conflict);

            var order = await orderRepository.GetByIdAsync(delivery.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (!await IsFarmerOwnerAsync(order))
                return Result<string>.Fail("Подтвердить доставку может только фермер — владелец заказа", ErrorType.Forbidden);

            return await ConfirmDeliveryWithPhotoAsync(delivery, order, photoStream, fileName, fileLength, "ConfirmManualDelivery",
                "Ручная доставка подтверждена по заказу №{0}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при подтверждении ручной доставки {DeliveryId}", deliveryId);
            return Result<string>.Fail("Не удалось подтвердить доставку", ErrorType.InternalServerError);
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

    // Блок 2 (2026-08-08, по явному запросу пользователя) — раньше отмену
    // доставки мог оформить только администратор (CancelAsync выше), у
    // курьера такой возможности не было вообще. Причина обязательна (см.
    // IAccountBlockService.RecordCancellationAsync — минимум несколько слов,
    // не одно слово) и учитывается в счётчике нарушений за 24ч.
    public async Task<Result<string>> CancelByCourierAsync(int deliveryId, string reason)
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
            if (delivery.Status is DeliveryStatus.Delivered or DeliveryStatus.Cancelled)
                return Result<string>.Fail("Нельзя отменить уже завершённую или отменённую доставку", ErrorType.Conflict);

            // Причину/лимит проверяем ДО мутации доставки — если превышен лимит
            // отмен и это создаёт новый бан, курьер всё равно узнаёт причину
            // отказа только если сама валидация причины не провалилась; сама
            // отмена доставки при этом происходит (бан не отменяет уже начатое
            // действие "я больше не могу её выполнить" — он лишь не даёт брать
            // НОВЫЕ доставки дальше).
            var recordResult = await accountBlockService.RecordCancellationAsync(currentUser.UserId.Value, "Courier", delivery.OrderId, reason);
            if (!recordResult.IsSuccess)
                return Result<string>.Fail(recordResult.Error!, recordResult.ErrorType ?? ErrorType.Validation);

            delivery.Status = DeliveryStatus.Cancelled;
            delivery.CancelledAt = DateTime.UtcNow;
            delivery.CancellationReason = reason.Trim();
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            var order = await orderRepository.GetByIdAsync(delivery.OrderId);
            if (order is not null)
            {
                if (order.Status is OrderStatus.CourierAssigned or OrderStatus.PickedUp or OrderStatus.InDelivery)
                {
                    order.Status = OrderStatus.ReadyForPickup;
                    await orderRepository.UpdateAsync(order);
                }

                var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
                var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
                if (farmerProfile is not null)
                    await NotifyAsync(farmerProfile.UserId, "Курьер отменил доставку", $"Курьер отменил доставку по заказу №{order.OrderNumber}: {reason.Trim()}");
                if (customerProfile is not null)
                    await NotifyAsync(customerProfile.UserId, "Доставка отменена", $"Курьер отменил доставку по вашему заказу №{order.OrderNumber}, фермер назначит нового.");
            }

            await CreateAuditLogAsync("CourierCancelDelivery", delivery.Id, $"Курьер отменил доставку: {reason.Trim()}");

            var successMessage = "Доставка отменена";
            if (recordResult.Data is not null)
                successMessage += $". {recordResult.Data}";

            return Result<string>.Ok(successMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при отмене доставки курьером {Id}", deliveryId);
            return Result<string>.Fail("Не удалось отменить доставку", ErrorType.InternalServerError);
        }
    }

    private static string FormatBlockMessage(Domain.Entities.AccountBlock block) =>
        $"Аккаунт временно заблокирован до {block.BlockedUntil:dd.MM.yyyy HH:mm} UTC. Причина: {block.Reason}.";

    public async Task<Result<string>> MarkReadyForPickupAsync(int orderId)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(orderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (!await IsFarmerOwnerAsync(order))
                return Result<string>.Fail("Нет доступа к этому заказу", ErrorType.Forbidden);

            var delivery = await deliveryRepository.GetByOrderIdAsync(orderId);
            if (delivery is null)
                return Result<string>.Fail("Сначала нужно назначить курьера", ErrorType.Conflict);

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

            // Блок 2 (2026-08-08) — заблокированный за частые отмены курьер не
            // может брать новые доставки, даже если она уже была назначена ему
            // ДО блокировки (второй, независимый от списка "доступных
            // курьеров" барьер — см. GetAvailableCouriersAsync ниже).
            var activeBlock = await accountBlockService.GetActiveBlockAsync(currentUser.UserId.Value);
            if (activeBlock is not null)
                return Result<string>.Fail(FormatBlockMessage(activeBlock), ErrorType.Forbidden);

            var delivery = await deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery is null)
                return Result<string>.Fail("Доставка не найдена", ErrorType.NotFound);
            if (delivery.CourierId != courierProfile.Id)
                return Result<string>.Fail("Эта доставка назначена не вам", ErrorType.Forbidden);
            if (delivery.Status != DeliveryStatus.Assigned)
                return Result<string>.Fail("Принять можно только только что назначенную доставку", ErrorType.Conflict);

            var orderForGuard = await orderRepository.GetByIdAsync(delivery.OrderId);
            // Порядок обязателен (по прямому запросу пользователя, 2026-08-04):
            // курьер не может "принять" доставку, пока фермер официально не
            // отдал заказ курьеру. На практике Delivery создаётся только внутри
            // AssignCourierAsync, который сам выставляет HandedToCourier, так
            // что это защита от рассинхронизации, а не реально достижимый
            // путь — но проверка запрошена явно.
            if (orderForGuard is not null && orderForGuard.FarmerStatus != FarmerOrderStatus.HandedToCourier)
                return Result<string>.Fail("Заказ ещё не передан курьеру фермером", ErrorType.Validation);

            delivery.Status = DeliveryStatus.Accepted;
            delivery.AcceptedAt = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            var order = orderForGuard;
            if (order is not null)
            {
                // CourierStatus — исключительно курьерское поле (см. комментарий
                // на Order.FarmerStatus/CourierStatus) — "Принял" фиксируется
                // именно здесь, в момент реального принятия доставки.
                order.CourierStatus = CourierOrderStatus.Accepted;
                await orderRepository.UpdateAsync(order);

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
            // InTransit — единственная реальная цель перехода теперь (см.
            // CourierTransitions), это и есть момент "забрал заказ".
            if (dto.Status is DeliveryStatus.PickedUp or DeliveryStatus.InTransit)
                delivery.PickedUpAt = DateTime.UtcNow;
            delivery.UpdatedAt = DateTime.UtcNow;
            await deliveryRepository.UpdateAsync(delivery);

            var order = await orderRepository.GetByIdAsync(delivery.OrderId);
            if (order is not null)
            {
                // Раньше здесь писали order.Status = PickedUp/InDelivery — по
                // прямому запросу пользователя (2026-08-04) эти промежуточные
                // шаги больше НЕ попадают в Order (ни в Status, ни в
                // CourierStatus, который сознательно грубее): вся гранулярность
                // курьерского пути остаётся только в Delivery.Status, чтобы
                // это поле правил единолично курьерский путь и никогда не
                // писалось из фермерского кода, и наоборот.
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

    public async Task<Result<string>> ConfirmDeliveryAsync(int deliveryId, Stream photoStream, string fileName, long fileLength)
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
            // ArrivedAtClient оставлен как валидный статус для подтверждения —
            // не ломать доставки, уже дошедшие до него до упрощения флоу
            // (2026-08-05, см. CourierTransitions) — но новый флоу подтверждает
            // фото сразу из InTransit, отдельного "прибыл к покупателю" больше нет.
            if (delivery.Status != DeliveryStatus.InTransit && delivery.Status != DeliveryStatus.ArrivedAtClient)
                return Result<string>.Fail("Сначала отметьте, что забрали заказ", ErrorType.Conflict);

            var order = await orderRepository.GetByIdAsync(delivery.OrderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            var result = await ConfirmDeliveryWithPhotoAsync(delivery, order, photoStream, fileName, fileLength, "ConfirmDelivery",
                "Доставка подтверждена по заказу №{0}");

            if (result.IsSuccess)
            {
                var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
                if (farmerProfile is not null)
                    await NotifyAsync(farmerProfile.UserId, "Заказ доставлен", $"Заказ №{order.OrderNumber} успешно доставлен покупателю.");
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при подтверждении доставки {Id}", deliveryId);
            return Result<string>.Fail("Не удалось подтвердить доставку", ErrorType.InternalServerError);
        }
    }

    // Общая логика фото-подтверждения доставки (2026-08-05) — раньше
    // ConfirmDeliveryAsync/ConfirmManualDeliveryAsync дублировали почти
    // идентичную проверку кода; теперь загрузка фото и завершение заказа
    // вынесены сюда, вызывающий код отвечает только за проверку владения/роли.
    private async Task<Result<string>> ConfirmDeliveryWithPhotoAsync(
        Delivery delivery, Order order, Stream photoStream, string fileName, long fileLength,
        string auditAction, string auditMessageTemplate)
    {
        if (delivery.Status == DeliveryStatus.Delivered)
            return Result<string>.Fail("Доставка уже подтверждена", ErrorType.Conflict);
        if (delivery.Status == DeliveryStatus.Cancelled)
            return Result<string>.Fail("Доставка отменена", ErrorType.Conflict);

        var validation = FileUploadValidator.ValidateImage(fileName, fileLength);
        if (validation is not null)
            return validation;

        var photoUrl = await fileStorageService.SaveAsync(photoStream, fileName, $"delivery-proof/{delivery.Id}");

        delivery.DeliveryProofPhotoUrl = photoUrl;
        delivery.Status = DeliveryStatus.Delivered;
        delivery.DeliveredAt = DateTime.UtcNow;
        delivery.UpdatedAt = DateTime.UtcNow;
        await deliveryRepository.UpdateAsync(delivery);

        order.CourierStatus = CourierOrderStatus.Delivered;
        await orderRepository.UpdateAsync(order);
        await CompleteOrderAfterDeliveryAsync(order.Id);

        var customerProfile = await customerProfileRepository.GetByIdAsync(order.CustomerId);
        if (customerProfile is not null)
            await NotifyAsync(customerProfile.UserId, "Заказ доставлен", $"Заказ №{order.OrderNumber} отмечен как доставленный.");

        await CreateAuditLogAsync(auditAction, delivery.Id, string.Format(auditMessageTemplate, order.OrderNumber));

        return Result<string>.Ok("Доставка подтверждена");
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

    // Не проваливает подтверждение доставки, если завершение заказа/начисление
    // фермеру споткнулось — доставка уже физически подтверждена курьером/
    // фермером, это тот же принцип "не роняем основное действие", что и у
    // NotifyAsync выше.
    private async Task CompleteOrderAfterDeliveryAsync(int orderId)
    {
        try
        {
            var result = await orderService.CompleteAfterDeliveryAsync(orderId);
            if (!result.IsSuccess)
                logger.LogWarning("Не удалось автозавершить заказ {OrderId} после доставки: {Error}", orderId, result.Error);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при автозавершении заказа {OrderId} после доставки", orderId);
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
            ManualCourierName = delivery.ManualCourierName,
            ManualCourierPhone = delivery.ManualCourierPhone,
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
            DeliveryProofPhotoUrl = delivery.DeliveryProofPhotoUrl,
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
            dto.FarmerStatus = order.FarmerStatus;
            dto.CourierStatus = order.CourierStatus;

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

        }

        return dto;
    }
}
