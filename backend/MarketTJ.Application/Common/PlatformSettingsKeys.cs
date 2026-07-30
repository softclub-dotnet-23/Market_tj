namespace MarketTJ.Application.Common;

// Структурированные "общие настройки платформы" (Admin → Настройки) хранятся
// как обычные AppSetting-строки под этими фиксированными ключами — переиспользуем
// уже существующий generic key-value CRUD (AppSetting/IAppSettingRepository,
// с Redis-кэшем и аудитом UpdatedByAdminId) вместо отдельной сущности+таблицы:
// набор полей закрытый и известный заранее, поэтому произвольность key-value
// тут не нужна, но плюсы существующего хранилища (кэш, аудит, миграция уже есть)
// перевешивают выгоду от типизированной таблицы на 11 колонок.
public static class PlatformSettingsKeys
{
    public const string SiteName = "platform.site_name";
    public const string LogoUrl = "platform.logo_url";
    public const string ContactEmail = "platform.contact_email";
    public const string ContactPhone = "platform.contact_phone";
    public const string CommissionPercent = "platform.commission_percent";
    public const string Currency = "platform.currency";
    public const string MinimumOrderAmount = "platform.minimum_order_amount";
    public const string MaintenanceModeEnabled = "platform.maintenance_mode_enabled";
    public const string MaintenanceMessage = "platform.maintenance_message";
    public const string EmailNotificationsEnabled = "platform.notifications_email_enabled";
    public const string SmsNotificationsEnabled = "platform.notifications_sms_enabled";

    public const string CategoryGeneral = "General";
    public const string CategoryCommission = "Commission";
    public const string CategoryNotifications = "Notifications";
    public const string CategoryMaintenance = "Maintenance";

    // Category+Description записываются в AppSetting при первом создании ключа
    // (см. PlatformSettingsService.UpsertAsync) — единый источник правды для
    // обоих мест, где это нужно (сервис при апсерте, тесты/сидер при желании
    // проверить полноту набора ключей).
    public static readonly IReadOnlyList<(string Key, string Category, string Description)> All =
    [
        (SiteName, CategoryGeneral, "Название сайта, отображаемое в шапке и письмах"),
        (LogoUrl, CategoryGeneral, "URL логотипа платформы"),
        (ContactEmail, CategoryGeneral, "Контактный email для связи с платформой"),
        (ContactPhone, CategoryGeneral, "Контактный телефон для связи с платформой"),
        (CommissionPercent, CategoryCommission, "Процент комиссии платформы с продаж фермеров"),
        (Currency, CategoryCommission, "Валюта платформы (код, например TJS)"),
        (MinimumOrderAmount, CategoryCommission, "Минимальная сумма заказа"),
        (MaintenanceModeEnabled, CategoryMaintenance, "Режим обслуживания включён/выключен"),
        (MaintenanceMessage, CategoryMaintenance, "Сообщение пользователям в режиме обслуживания"),
        (EmailNotificationsEnabled, CategoryNotifications, "Email-уведомления включены/выключены"),
        (SmsNotificationsEnabled, CategoryNotifications, "SMS-уведомления включены/выключены"),
    ];
}
