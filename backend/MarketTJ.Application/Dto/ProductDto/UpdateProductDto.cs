namespace MarketTJ.Application.Dto.ProductDto;

public class UpdateProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Unit { get; set; } = null!;
    public bool IsActive { get; set; }
}
