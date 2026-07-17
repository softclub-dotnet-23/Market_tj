namespace MarketTJ.Application.Dto.AppSettingDto;

public class GetAppSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedByAdminId { get; set; }
}
