namespace MarketTJ.Application.Dto.CategoryDto;

public class UpdateCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? NameTj { get; set; }
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}
