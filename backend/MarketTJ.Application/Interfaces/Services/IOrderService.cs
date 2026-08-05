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

    // Гибридная оплата: подтверждение оплаты наличными при доставке (только
    // для PaymentMethod == CashOnDelivery) — фермер (владелец заказа) или
    // админ, см. OrderService.MarkPaidAsync.
    Task<Result<string>> MarkPaidAsync(int id);

    // По прямому запросу пользователя (2026-08-05): раньше заказ завершал
    // администратор вручную статусом Completed — теперь Admin почти не
    // участвует в заказе, поэтому заказ завершается САМ, как только курьер
    // (обычный или "ручной") подтвердил доставку — см. DeliveryService.
    // ConfirmDeliveryAsync/ConfirmManualDeliveryAsync. Без проверки прав —
    // вызывается только изнутри бэкенда, после того как сам вызывающий метод
    // уже проверил владение доставкой.
    Task<Result<string>> CompleteAfterDeliveryAsync(int orderId);
}
