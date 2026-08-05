using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.ProductListingDto;

public class CreateProductListingDto
{
    public int FarmerProfileId { get; set; }
    public int CategoryId { get; set; }
    public string Unit { get; set; } = null!;
    // Nullable — фермер обязан заполнить МИНИМУМ один язык (см.
    // ProductListingValidator), остальные (включая Title/русский)
    // автоматически переводятся через Groq (ProductListingService).
    public string? Title { get; set; }
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
}
