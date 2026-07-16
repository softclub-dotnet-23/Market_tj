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

    // CustomerProfile 1 — many Order / FarmerProfile 1 — many Order
    // (раздел 9 TZ1.md — снова через профили, не напрямую через User).
    public CustomerProfile Customer { get; set; } = null!;
    public FarmerProfile Farmer { get; set; } = null!;

    // Order 1 — many OrderItem.
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    // Order 1 — 0..1 Delivery / Review / DeliverySlot / Conversation.
    public Delivery? Delivery { get; set; }
    public Review? Review { get; set; }
    public DeliverySlot? DeliverySlot { get; set; }
    public Conversation? Conversation { get; set; }

    // Order 1 — 0..many Payment / RefundRequest.
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
}
