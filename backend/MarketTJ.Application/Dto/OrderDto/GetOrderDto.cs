using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.OrderDto;

public class GetOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    // Резолвится на сервере через CustomerProfile.UserId → User (раздел 8.10
    // ТЗ описывает только CustomerId; имя/телефон покупателя нужны, чтобы
    // фермер и админ реально могли понять, для кого заказ, и связаться с
    // покупателем — без этого /api/users доступен только Admin, а фермеру
    // взять имя покупателя было неоткуда).
    public string? CustomerFullName { get; set; }
    public string? CustomerPhone { get; set; }
    // Status — вычисляемое поле для обратной совместимости (те же значения,
    // что фронтенд уже знает и умеет отображать), см. OrderService.ComputeDisplayStatus.
    // FarmerStatus/CourierStatus — реальные, независимо пишущиеся поля,
    // добавлены по прямому запросу пользователя (2026-08-04) для тех, кому
    // нужна явная раздельная правда, а не вычисленная сводка.
    public OrderStatus Status { get; set; }
    public FarmerOrderStatus? FarmerStatus { get; set; }
    public CourierOrderStatus? CourierStatus { get; set; }
    public string DeliveryAddress { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public string? CustomerComment { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public OrderPaymentMethod PaymentMethod { get; set; }
    public bool IsPaid { get; set; }
    public int? WalletId { get; set; }
}
