using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public int CustomerId { get; set; }
    public int FarmerId { get; set; }
    public OrderStatus Status { get; set; }

    // Разделены по прямому запросу пользователя (2026-08-04) — раньше и
    // фермер, и курьер писали в общий Status (DeliveryService трогал его
    // из courier-эндпоинтов наравне с OrderService), что создавало риск
    // перезаписи. FarmerStatus пишет только фермерский путь (OrderService,
    // DeliveryService.AssignCourierAsync), CourierStatus — только курьерский
    // (DeliveryService.AcceptAsync/ConfirmDeliveryAsync). Оба nullable —
    // null, пока соответствующая сторона ещё не действовала. Старый Status
    // остаётся источником истины для Pending/Accepted/Rejected/Preparing/
    // Completed/Cancelled (в т.ч. комиссия в ApplyWalletEffectsForStatusChangeAsync
    // завязана именно на него) — см. миграцию AddFarmerCourierOrderStatus.
    public FarmerOrderStatus? FarmerStatus { get; set; }
    public CourierOrderStatus? CourierStatus { get; set; }

    public string DeliveryAddress { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public string? CustomerComment { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryPrice { get; set; }
    public decimal TotalAmount { get; set; }

    public OrderPaymentMethod PaymentMethod { get; set; }

    // Card — становится true сразу при создании (списание уже прошло).
    // CashOnDelivery — false при создании, курьер/фермер отмечает true через
    // отдельный эндпоинт после получения наличных при доставке (см.
    // OrderService.MarkPaidAsync). Начисление фермеру за CashOnDelivery-заказ
    // при Completed происходит только если IsPaid == true — иначе платформа
    // начислила бы комиссию с денег, которые ещё не подтверждены как полученные.
    public bool IsPaid { get; set; }

    // Какая именно карта была списана при оплате Card — нужно для отображения
    // ("оплачено картой •••• 1234") и как страховочная ссылка в дополнение к
    // WalletTransaction.RelatedOrderId. Null для CashOnDelivery.
    public int? WalletId { get; set; }

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
