namespace MarketTJ.Application.Dto.PlatformSettingsDto;

public class UpdatePlatformSettingsDto
{
    public string SiteName { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string ContactEmail { get; set; } = null!;
    public string ContactPhone { get; set; } = null!;

    public decimal CommissionPercent { get; set; }
    public string Currency { get; set; } = null!;
    public decimal MinimumOrderAmount { get; set; }

    public bool MaintenanceModeEnabled { get; set; }
    public string? MaintenanceMessage { get; set; }

    public bool EmailNotificationsEnabled { get; set; }
    public bool SmsNotificationsEnabled { get; set; }
}
