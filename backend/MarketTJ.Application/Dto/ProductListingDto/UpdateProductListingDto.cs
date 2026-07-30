using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.ProductListingDto;

public class UpdateProductListingDto
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    public int CategoryId { get; set; }
    public string Unit { get; set; } = null!;
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
}
