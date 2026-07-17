namespace MarketTJ.Application.Dto.CategoryDto;

public class CreateCategoryDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}
