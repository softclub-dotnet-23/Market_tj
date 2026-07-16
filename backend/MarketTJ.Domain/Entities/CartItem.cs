namespace MarketTJ.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductListingId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // User 1 — many CartItem (как Customer) / ProductListing 1 — many CartItem.
    public User Customer { get; set; } = null!;
    public ProductListing ProductListing { get; set; } = null!;
}
