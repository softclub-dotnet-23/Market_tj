using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class Order
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
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // User 1 — many Order (как Customer / как Farmer) — два разных FK на User.
    public User Customer { get; set; } = null!;
    public User Farmer { get; set; } = null!;

    // Order 1 — many OrderItem.
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    // Order 1 — 0..1 Delivery / Review.
    public Delivery? Delivery { get; set; }
    public Review? Review { get; set; }
}
