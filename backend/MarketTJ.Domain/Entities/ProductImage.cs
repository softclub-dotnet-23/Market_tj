namespace MarketTJ.Domain.Entities;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductListingId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public bool IsMain { get; set; }
    public DateTime CreatedAt { get; set; }

    // ProductListing 1 — many ProductImage.
    public ProductListing ProductListing { get; set; } = null!;
}
