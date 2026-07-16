namespace MarketTJ.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductListingId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // CustomerProfile 1 — many CartItem / ProductListing 1 — many CartItem
    // (раздел 9 TZ1.md — снова через CustomerProfile, не напрямую через User).
    public CustomerProfile Customer { get; set; } = null!;
    public ProductListing ProductListing { get; set; } = null!;
}
