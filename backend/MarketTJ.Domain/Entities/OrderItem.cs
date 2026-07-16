namespace MarketTJ.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductListingId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }

    // Order 1 — many OrderItem / ProductListing 1 — many OrderItem.
    // Как и с CartItem — визуально Order↔ProductListing выглядит как
    // "многие ко многим", но раздел 9 явно моделирует её через OrderItem
    // (с собственными полями UnitPrice/Quantity/TotalPrice — копия на момент
    // заказа), поэтому это два reference-навигейшена, а не skip-navigation.
    public Order Order { get; set; } = null!;
    public ProductListing ProductListing { get; set; } = null!;
}
