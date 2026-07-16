namespace MarketTJ.Domain.Entities;

public class AppSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedByAdminId { get; set; }

    // User — Admin, обновивший настройку (необязательная связь).
    public User? UpdatedByAdmin { get; set; }
}
