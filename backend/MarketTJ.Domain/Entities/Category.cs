namespace MarketTJ.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Category 1 — many Product.
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
