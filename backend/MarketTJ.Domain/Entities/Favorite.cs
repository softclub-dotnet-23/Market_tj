namespace MarketTJ.Domain.Entities;

public class Favorite
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductListingId { get; set; }
    public DateTime CreatedAt { get; set; }

    // User 1 — many Favorite (как Customer) / ProductListing 1 — many Favorite —
    // раздел 9: "ProductListing many — many User (через Favorite)".
    public User Customer { get; set; } = null!;
    public ProductListing ProductListing { get; set; } = null!;
}
