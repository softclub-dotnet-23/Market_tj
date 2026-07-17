namespace MarketTJ.Application.Dto.ProductImageDto;

public class CreateProductImageDto
{
    public int ProductListingId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public bool IsMain { get; set; }
}
