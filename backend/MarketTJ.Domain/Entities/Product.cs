namespace MarketTJ.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Unit { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Category 1 — many Product.
    public Category Category { get; set; } = null!;

    // Product 1 — many ProductListing.
    public ICollection<ProductListing> ProductListings { get; set; } = new List<ProductListing>();
}
