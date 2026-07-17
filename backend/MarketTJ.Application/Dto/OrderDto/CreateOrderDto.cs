using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.OrderDto;

public class CreateOrderDto
{
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
}
