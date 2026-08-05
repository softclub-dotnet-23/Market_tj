using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.ProductListingDto;

public class GetProductListingDto
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    public int CategoryId { get; set; }
    public string Unit { get; set; } = null!;
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Раньше фронт сам тянул /product-images и /reviews целиком и джойнил их
    // в браузере на каждый список объявлений (см. audit 2026-08-02) — теперь
    // сервер отдаёт готовые значения на каждом read-эндпоинте объявлений
    // (GetAll/GetById/SearchCatalog), не только на SearchCatalog, как раньше.
    public List<string> ImageUrls { get; set; } = new();
    public double Rating { get; set; }
    public int OrderCount { get; set; }
}
