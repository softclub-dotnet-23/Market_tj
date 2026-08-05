using MarketTJ.Domain.Enums;

namespace MarketTJ.Domain.Entities;

public class ProductListing
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    // ProductId — устаревшая связь со справочником Product (Category выбирал
    // админ, конкретный товар — тоже админ через Product). По прямому запросу
    // категорию по-прежнему выбирает админ, а название товара и единицу
    // измерения теперь вводит сам фермер — поэтому CategoryId/Unit переехали
    // прямо на ProductListing, а ProductId стал необязательным (оставлен
    // nullable ради уже существующих объявлений, новые его не заполняют).
    public int? ProductId { get; set; }
    public int CategoryId { get; set; }
    public string Unit { get; set; } = null!;
    // Title/Description — русский (основной, обязательный) язык, по образцу
    // Category.Name/NameTj/NameEn. TitleTj/TitleEn/DescriptionTj/
    // DescriptionEn — nullable, автоматически переводятся через Groq
    // (ProductListingService), если фермер их не заполнил сам (2026-08-05).
    public string Title { get; set; } = null!;
    public string? TitleTj { get; set; }
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionTj { get; set; }
    public string? DescriptionEn { get; set; }
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

    // Product 1 — many ProductListing (устаревшая, см. ProductId) / Category 1 —
    // many ProductListing / FarmerProfile 1 — many ProductListing.
    public Product? Product { get; set; }
    public Category Category { get; set; } = null!;
    public FarmerProfile FarmerProfile { get; set; } = null!;

    // ProductListing 1 — many ProductImage / CartItem / OrderItem / ReportedListing / Favorite.
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<ReportedListing> Reports { get; set; } = new List<ReportedListing>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
