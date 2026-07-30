namespace MarketTJ.Application.Dto.PlatformSettingsDto;

public class GetPlatformSettingsDto
{
    // General
    public string SiteName { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string ContactEmail { get; set; } = null!;
    public string ContactPhone { get; set; } = null!;

    // Commission
    public decimal CommissionPercent { get; set; }
    public string Currency { get; set; } = null!;
    public decimal MinimumOrderAmount { get; set; }

    // Maintenance
    public bool MaintenanceModeEnabled { get; set; }
    public string? MaintenanceMessage { get; set; }

    // Notifications
    public bool EmailNotificationsEnabled { get; set; }
    public bool SmsNotificationsEnabled { get; set; }

    // Null, если ни одна настройка ещё ни разу не сохранялась (первый заход
    // на страницу — фронтенд получает дефолты, а не 404).
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByAdminId { get; set; }
}
