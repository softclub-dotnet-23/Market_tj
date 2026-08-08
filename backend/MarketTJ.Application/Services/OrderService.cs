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
    IWalletService walletService,
    ICurrentUserService currentUser,
    IAccountBlockService accountBlockService,
    ILogger<OrderService> logger) : IOrderService
{
    // Audit 2026-07-28, находка 2.2 (IDOR): Order.CustomerId/FarmerId — это Id
    // профилей (не User.Id), поэтому владение резолвится через них. Admin — всё;
    // Customer — свои заказы (CustomerId == его CustomerProfile.Id); Farmer —
    // заказы, сделанные у него (FarmerId == его FarmerProfile.Id).
    private async Task<bool> IsOwnerAsync(Order order)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;

        var customerProfile = await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        if (customerProfile is not null && customerProfile.Id == order.CustomerId)
            return true;

        var farmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return farmerProfile is not null && farmerProfile.Id == order.FarmerId;
    }

    // Строго "фермер — владелец заказа", БЕЗ пропуска для Admin — по прямому
    // запросу пользователя (2026-08-05): принять заказ (Status → Accepted)
    // может только фермер, администратор эту роль больше не дублирует.
    private async Task<bool> IsFarmerOwnerAsync(Order order)
    {
        if (currentUser.UserId is null)
            return false;

        var farmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return farmerProfile is not null && farmerProfile.Id == order.FarmerId;
    }

    public async Task<Result<IEnumerable<GetOrderDto>>> GetAllAsync()
    {
        try
        {
            var orders = await orderRepository.GetAllAsync();

            if (!currentUser.IsAdmin() && currentUser.UserId is not null)
            {
                var myCustomerProfile = await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                var myFarmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                orders = orders.Where(o =>
                    (myCustomerProfile is not null && o.CustomerId == myCustomerProfile.Id) ||
                    (myFarmerProfile is not null && o.FarmerId == myFarmerProfile.Id)).ToList();
            }
            else if (!currentUser.IsAdmin())
            {
                orders = [];
            }

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

            if (!await IsOwnerAsync(order))
                return Result<GetOrderDto?>.Fail("Нет доступа к этому заказу", ErrorType.Forbidden);

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

            // IDOR-guard (audit 2026-07-28, находка 2.2): CustomerId из тела
            // запроса не доверяется — Customer не может оформить заказ от имени
            // другого покупателя. Admin (например, создание заказа вручную через
            // Admin-панель) — исключение, для него dto.CustomerId используется как есть.
            var customerProfileId = dto.CustomerId;
            if (!currentUser.IsAdmin() && currentUser.UserId is not null)
            {
                var ownCustomerProfile = await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (ownCustomerProfile is null)
                    return Result<string>.Fail("Профиль покупателя не найден", ErrorType.NotFound);
                customerProfileId = ownCustomerProfile.Id;
            }

            var customerProfile = await customerProfileRepository.GetByIdAsync(customerProfileId);
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
                CustomerId = customerProfileId,
                FarmerId = dto.FarmerId,
                Status = OrderStatus.Pending,
                DeliveryAddress = dto.DeliveryAddress,
                Region = dto.Region,
                District = dto.District,
                CustomerComment = dto.CustomerComment,
                Subtotal = dto.Subtotal,
                DeliveryPrice = dto.DeliveryPrice,
                TotalAmount = dto.Subtotal + dto.DeliveryPrice,
                PaymentMethod = dto.PaymentMethod,
                // Card — оплачено сразу (списание ниже). CashOnDelivery —
                // ожидает оплаты наличными при получении (см. MarkPaidAsync).
                IsPaid = dto.PaymentMethod == OrderPaymentMethod.Card,
                WalletId = dto.PaymentMethod == OrderPaymentMethod.Card ? dto.WalletId : null,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await orderRepository.AddAsync(order);

            // Списание при оформлении заказа (не при более позднем этапе
            // жизненного цикла) — покупатель платит сразу, как только заказ
            // создан, а не когда фермер его примет. Только для Card — при
            // CashOnDelivery деньги не списываются вовсе, курьер/фермер
            // получает их физически при доставке (см. MarkPaidAsync), и эта
            // опция должна работать даже для покупателя без единой карты.
            // Если средств не хватает (или выбранная карта недоступна),
            // только что созданная запись удаляется полностью (не soft-delete —
            // платежа не было, заказа как бы не было вовсе), а не остаётся
            // висеть в базе неоплаченной.
            if (dto.PaymentMethod == OrderPaymentMethod.Card)
            {
                var debitResult = await walletService.DebitForOrderAsync(customerProfile.UserId, dto.WalletId!.Value, order.TotalAmount, order.Id);
                if (!debitResult.IsSuccess)
                {
                    await orderRepository.DeleteAsync(order);
                    return Result<string>.Fail(debitResult.Error ?? "Недостаточно средств для оплаты заказа", debitResult.ErrorType ?? ErrorType.Validation);
                }
            }

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

            if (!await IsOwnerAsync(order))
                return Result<string>.Fail("Нет доступа к этому заказу", ErrorType.Forbidden);

            // Раздел 10.4 ТЗ: завершённый заказ нельзя редактировать.
            if (order.Status == OrderStatus.Completed)
                return Result<string>.Fail("Завершённый заказ нельзя редактировать", ErrorType.Validation);

            // По прямому запросу пользователя (2026-08-05): принять заказ
            // может только фермер — владелец заказа, не Admin и не покупатель.
            if (dto.Status == OrderStatus.Accepted && order.Status != OrderStatus.Accepted && !await IsFarmerOwnerAsync(order))
                return Result<string>.Fail("Принять заказ может только фермер — владелец заказа", ErrorType.Forbidden);

            // Блок 2 (2026-08-08) — заблокированный (за частые немотивированные
            // отклонения) фермер не может принимать новые заказы, пока не
            // истечёт срок блокировки/его не разблокирует Admin вручную.
            if (dto.Status == OrderStatus.Accepted && order.Status != OrderStatus.Accepted && currentUser.UserId is not null)
            {
                var farmerBlock = await accountBlockService.GetActiveBlockAsync(currentUser.UserId.Value);
                if (farmerBlock is not null)
                    return Result<string>.Fail(farmerBlock.FormatBlockMessage(), ErrorType.Forbidden);
            }

            // Блок 2 (2026-08-08, по явному запросу пользователя) — по аналогии
            // с курьером (DeliveryService.CancelByCourierAsync): отклонение
            // заказа ИМЕННО фермером-владельцем требует причины (минимум
            // несколько слов) и учитывается в счётчике нарушений за 24ч. Admin
            // по-прежнему может выставить Rejected через отдельный
            // ChangeStatusAsync без этих ограничений — это другой, админский
            // путь, не входящий в правило "3 отказа фермера = бан".
            string? farmerRejectionNotice = null;
            var becomingRejectedByFarmer = order.Status != OrderStatus.Rejected && dto.Status == OrderStatus.Rejected;
            if (becomingRejectedByFarmer)
            {
                if (!await IsFarmerOwnerAsync(order))
                    return Result<string>.Fail("Отклонить заказ может только фермер — владелец заказа", ErrorType.Forbidden);

                var recordResult = await accountBlockService.RecordCancellationAsync(
                    currentUser.UserId!.Value, "Farmer", order.Id, dto.RejectionReason ?? string.Empty);
                if (!recordResult.IsSuccess)
                    return Result<string>.Fail(recordResult.Error!, recordResult.ErrorType ?? ErrorType.Validation);
                farmerRejectionNotice = recordResult.Data;
            }

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

            // CustomerId/FarmerId сознательно не перезаписываются из dto —
            // владелец заказа не должен меняться через Update (audit 2026-07-28,
            // находка 2.2); проверки существования профилей выше остаются как
            // валидация формы запроса.
            order.OrderNumber = dto.OrderNumber;
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
            // FarmerStatus — отдельное поле, которое пишет только фермерский
            // путь (по прямому запросу пользователя, 2026-08-04): фиксируем
            // "Принят" тем же движением, что и Status → Accepted.
            if (dto.Status == OrderStatus.Accepted)
                order.FarmerStatus = FarmerOrderStatus.Accepted;

            await orderRepository.UpdateAsync(order);

            if (becameRejectedOrCancelled)
                await RestoreStockForOrderAsync(id);

            await ApplyWalletEffectsForStatusChangeAsync(order, previousStatus, dto.Status);

            var successMessage = "Заказ обновлён";
            if (farmerRejectionNotice is not null)
                successMessage += $". {farmerRejectionNotice}";

            return Result<string>.Ok(successMessage);
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

            if (!await IsOwnerAsync(order))
                return Result<string>.Fail("Нет доступа к этому заказу", ErrorType.Forbidden);

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

            // По прямому запросу пользователя (2026-08-05): этот метод вызывает
            // только Admin (см. AdminOrderController) — принимать заказ должен
            // исключительно фермер (UpdateAsync/IsFarmerOwnerAsync), эта админ-
            // ручка больше не может выставить Accepted вообще.
            if (status == OrderStatus.Accepted)
                return Result<string>.Fail("Принять заказ может только фермер — владелец заказа", ErrorType.Forbidden);

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
            if (status == OrderStatus.Accepted)
                order.FarmerStatus = FarmerOrderStatus.Accepted;

            await orderRepository.UpdateAsync(order);

            await ApplyWalletEffectsForStatusChangeAsync(order, previousStatus, status);

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

    // Раздел "гибридная оплата": курьер физически принимает Delivery (свою
    // сущность, не Order), а статус/оплата САМОГО заказа — прерогатива
    // Farmer/Admin, как и весь остальной жизненный цикл Order в этом
    // проекте (см. IsOwnerAsync — Courier туда никогда не допускается, у
    // него своя авторизация через Delivery.CourierId). Поэтому "отметить
    // оплаченным" здесь — действие фермера (владельца заказа) или админа,
    // не курьера.
    public async Task<Result<string>> MarkPaidAsync(int id)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(id);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (!currentUser.IsAdmin())
            {
                if (currentUser.UserId is null)
                    return Result<string>.Fail("Нет доступа к этому заказу", ErrorType.Forbidden);

                var farmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
                if (farmerProfile is null || farmerProfile.Id != order.FarmerId)
                    return Result<string>.Fail("Нет доступа к этому заказу", ErrorType.Forbidden);
            }

            if (order.PaymentMethod != OrderPaymentMethod.CashOnDelivery)
                return Result<string>.Fail("Этот заказ не оплачивается наличными", ErrorType.Validation);

            if (order.IsPaid)
                return Result<string>.Ok("Заказ уже отмечен как оплаченный");

            order.IsPaid = true;
            await orderRepository.UpdateAsync(order);

            // Если заказ уже был завершён ДО подтверждения оплаты наличными
            // (маловероятный, но возможный порядок действий) — начислить
            // фермеру сейчас, т.к. в ChangeStatusAsync/UpdateAsync начисление
            // было пропущено именно из-за IsPaid == false на тот момент.
            if (order.Status == OrderStatus.Completed)
            {
                var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
                if (farmerProfile is not null)
                {
                    var creditResult = await walletService.CreditFarmerForOrderAsync(farmerProfile.UserId, order.Subtotal, order.Id);
                    if (!creditResult.IsSuccess)
                        logger.LogError("Не удалось начислить фермеру за заказ {OrderId} после подтверждения оплаты наличными: {Error}", order.Id, creditResult.Error);
                }
            }

            return Result<string>.Ok("Оплата наличными подтверждена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при подтверждении оплаты наличными для заказа {Id}", id);
            return Result<string>.Fail("Не удалось подтвердить оплату", ErrorType.InternalServerError);
        }
    }

    // По прямому запросу пользователя (2026-08-05): заказ завершается сам,
    // как только доставка подтверждена (курьером или фермером — для "ручного"
    // курьера), без отдельного шага Admin. Вызывается изнутри DeliveryService
    // после успешного ConfirmDeliveryAsync/ConfirmManualDeliveryAsync — права
    // уже проверены там (и на доставку, и через владение заказом), здесь
    // повторной проверки нет намеренно. Идемпотентно: если заказ уже
    // Completed/Cancelled/Rejected, ничего не делает и не считается ошибкой.
    public async Task<Result<string>> CompleteAfterDeliveryAsync(int orderId)
    {
        try
        {
            var order = await orderRepository.GetByIdAsync(orderId);
            if (order is null)
                return Result<string>.Fail("Заказ не найден", ErrorType.NotFound);

            if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Rejected)
                return Result<string>.Ok("Заказ уже закрыт");

            var previousStatus = order.Status;
            order.Status = OrderStatus.Completed;
            order.CompletedAt ??= DateTime.UtcNow;
            await orderRepository.UpdateAsync(order);

            await ApplyWalletEffectsForStatusChangeAsync(order, previousStatus, OrderStatus.Completed);

            return Result<string>.Ok("Заказ завершён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при автозавершении заказа {OrderId} после доставки", orderId);
            return Result<string>.Fail("Не удалось завершить заказ", ErrorType.InternalServerError);
        }
    }

    // Единая точка для обоих мест, где меняется статус заказа (ChangeStatusAsync
    // и UpdateAsync) — начисление фермеру при завершении заказа, возврат
    // покупателю при отклонении/отмене. Списание при этом происходит один
    // раз, при СОЗДАНИИ заказа (см. CreateAsync) — Completed достигается
    // только из состояний до него, поэтому фермер начисляется ровно один
    // раз за заказ, а Cancelled/Rejected после Completed невозможны (см.
    // проверку "завершённый заказ нельзя редактировать" выше) — двойного
    // начисления/возврата за один и тот же заказ произойти не может.
    // Ошибка кошелька здесь намеренно не проваливает саму смену статуса
    // (заказ уже физически выполнен/отменён) — только логируется, как и
    // сбой уведомления на фронтенде при оформлении заказа.
    private async Task ApplyWalletEffectsForStatusChangeAsync(Order order, OrderStatus previousStatus, OrderStatus newStatus)
    {
        if (newStatus == OrderStatus.Completed && previousStatus != OrderStatus.Completed)
        {
            // CashOnDelivery, ещё не оплаченный наличными физически (IsPaid ==
            // false) — платформа не может начислить фермеру комиссию с денег,
            // которые ещё не подтверждены как полученные (см. MarkPaidAsync).
            // Card всегда IsPaid == true с момента создания заказа.
            if (order.PaymentMethod == OrderPaymentMethod.CashOnDelivery && !order.IsPaid)
            {
                logger.LogInformation("Заказ {OrderId} завершён наличными без подтверждения оплаты — начисление фермеру отложено до MarkPaidAsync", order.Id);
                return;
            }

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(order.FarmerId);
            if (farmerProfile is null) return;

            var creditResult = await walletService.CreditFarmerForOrderAsync(farmerProfile.UserId, order.Subtotal, order.Id);
            if (!creditResult.IsSuccess)
                logger.LogError("Не удалось начислить фермеру за заказ {OrderId}: {Error}", order.Id, creditResult.Error);
        }
        else if ((newStatus == OrderStatus.Rejected || newStatus == OrderStatus.Cancelled) &&
                 previousStatus != OrderStatus.Rejected && previousStatus != OrderStatus.Cancelled)
        {
            // RefundForOrderAsync сам находит нужную карту по исходной
            // Purchase-транзакции заказа и no-op'ается, если списания не было
            // (CashOnDelivery) — явная проверка PaymentMethod здесь не нужна,
            // но избавляет от лишнего похода в БД в самом частом случае.
            if (order.PaymentMethod != OrderPaymentMethod.Card)
                return;

            var refundResult = await walletService.RefundForOrderAsync(order.Id);
            if (!refundResult.IsSuccess)
                logger.LogError("Не удалось вернуть средства покупателю за заказ {OrderId}: {Error}", order.Id, refundResult.Error);
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

    // Сводный статус для покупателя/фронтенда — вычисляется из
    // Status+FarmerStatus+CourierStatus, а не хранится отдельно (по прямому
    // запросу пользователя, 2026-08-04, п.4 "разделения статусов"). Значения
    // намеренно взяты из уже существующего OrderStatus (не новый enum) —
    // фронтенд (ORDER_STATUS_KEYS/CLASSES/ICONS в lib/orderStatus.ts) уже
    // умеет их отображать, менять справочники там не нужно.
    private static OrderStatus ComputeDisplayStatus(Order order)
    {
        if (order.Status is not (OrderStatus.Accepted or OrderStatus.Preparing or OrderStatus.ReadyForPickup
            or OrderStatus.CourierAssigned or OrderStatus.PickedUp or OrderStatus.InDelivery or OrderStatus.Delivered))
            return order.Status;

        if (order.CourierStatus == CourierOrderStatus.Delivered)
            return OrderStatus.Delivered;
        if (order.CourierStatus == CourierOrderStatus.Accepted)
            return OrderStatus.InDelivery;
        if (order.FarmerStatus == FarmerOrderStatus.HandedToCourier)
            return OrderStatus.ReadyForPickup;

        return order.Status;
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
            Status = ComputeDisplayStatus(order),
            FarmerStatus = order.FarmerStatus,
            CourierStatus = order.CourierStatus,
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
            CustomerPhone = customer.Phone,
            PaymentMethod = order.PaymentMethod,
            IsPaid = order.IsPaid,
            WalletId = order.WalletId
        };
    }
}
