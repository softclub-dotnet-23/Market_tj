namespace MarketTJ.Application.Dto.ProductImageDto;

public class GetProductImageDto
{
    public int Id { get; set; }
    public int ProductListingId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public bool IsMain { get; set; }
    public DateTime CreatedAt { get; set; }
}
