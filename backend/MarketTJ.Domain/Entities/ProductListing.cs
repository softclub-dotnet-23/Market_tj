using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class ProductListing
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    public int ProductId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public decimal RetailPricePerKg { get; set; }
    public decimal? WholesalePricePerKg { get; set; }
    public decimal? WholesaleMinimumQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public DateTime? HarvestDate { get; set; }
    public DateTime? ExpectedHarvestDate { get; set; }
    public string QualityGrade { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public string Address { get; set; } = null!;
    public ListingStatus Status { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Product 1 — many ProductListing / FarmerProfile 1 — many ProductListing.
    public Product Product { get; set; } = null!;
    public FarmerProfile FarmerProfile { get; set; } = null!;

    // ProductListing 1 — many ProductImage / CartItem / OrderItem / ReportedListing / Favorite.
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<ReportedListing> Reports { get; set; } = new List<ReportedListing>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
