namespace MarketTJ.Application.Dto.CategoryDto;

public class GetCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? NameTj { get; set; }
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Сколько активных объявлений подтверждённых фермеров в этой категории —
    // раньше фронт считал сам, стянув себе весь каталог (audit 2026-08-02).
    public int ActiveListingCount { get; set; }
}
