namespace MarketTJ.Application.Dto.AppSettingDto;

public class CreateAppSettingDto
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? Description { get; set; }
    public int? UpdatedByAdminId { get; set; }
}
