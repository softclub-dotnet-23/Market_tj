using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.OrderDto;

public class UpdateOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public OrderStatus Status { get; set; }
    public string DeliveryAddress { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public string? CustomerComment { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // Блок 2 (2026-08-08, по явному запросу пользователя) — обязательна,
    // когда фермер сам переводит заказ в Rejected (см. OrderService.UpdateAsync);
    // для остальных переходов статуса игнорируется.
    public string? RejectionReason { get; set; }
}
