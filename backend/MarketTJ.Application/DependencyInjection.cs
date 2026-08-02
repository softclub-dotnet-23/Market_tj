using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarketTJ.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AddHttpClient<TInterface, TImplementation> сам регистрирует
        // IAiAssistantService со scoped-совместимым временем жизни и внедряет
        // сконфигурированный HttpClient в конструктор. Без явного Timeout
        // дефолт HttpClient — 100 секунд: при недоступности Groq API
        // запрос пользователя к AI-ассистенту завис бы почти на две минуты
        // вместо быстрой понятной ошибки.
        services.AddHttpClient<IAiAssistantService, AiAssistantService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<IReviewAutoReplyService, ReviewAutoReplyService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFarmerProfileService, FarmerProfileService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<ICourierProfileService, CourierProfileService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductListingService, ProductListingService>();
        services.AddScoped<IProductImageService, ProductImageService>();
        services.AddScoped<ICartItemService, CartItemService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderItemService, OrderItemService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IChatMessageService, ChatMessageService>();
        services.AddScoped<IAppSettingService, AppSettingService>();
        services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();
        services.AddScoped<IFarmerDocumentService, FarmerDocumentService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IReportedListingService, ReportedListingService>();
        services.AddScoped<IRefundRequestService, RefundRequestService>();
        services.AddScoped<IDeliveryZoneService, DeliveryZoneService>();
        services.AddScoped<ICommissionService, CommissionService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IFarmerStaffMemberService, FarmerStaffMemberService>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();
        services.AddScoped<ISupportMessageService, SupportMessageService>();
        services.AddScoped<IDeliverySlotService, DeliverySlotService>();
        services.AddScoped<IDailySalesSnapshotService, DailySalesSnapshotService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();

        return services;
    }
}
