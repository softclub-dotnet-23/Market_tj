using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Infrastructure.Caching;
using MarketTJ.Infrastructure.Email;
using MarketTJ.Infrastructure.Persistence;
using MarketTJ.Infrastructure.Persistence.Repositories;
using MarketTJ.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketTJ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("RedisCache");
        });
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IFarmerProfileRepository, FarmerProfileRepository>();
        services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
        services.AddScoped<ICourierProfileRepository, CourierProfileRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductListingRepository, ProductListingRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<ICartItemRepository, CartItemRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderItemRepository, OrderItemRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IFarmerDocumentRepository, FarmerDocumentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IReportedListingRepository, ReportedListingRepository>();
        services.AddScoped<IRefundRequestRepository, RefundRequestRepository>();
        services.AddScoped<IDeliveryZoneRepository, DeliveryZoneRepository>();
        services.AddScoped<ICommissionRepository, CommissionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IFarmerStaffMemberRepository, FarmerStaffMemberRepository>();
        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ISupportMessageRepository, SupportMessageRepository>();
        services.AddScoped<IDeliverySlotRepository, DeliverySlotRepository>();
        services.AddScoped<IDailySalesSnapshotRepository, DailySalesSnapshotRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IEmailVerificationCodeRepository, EmailVerificationCodeRepository>();

        // Railway блокирует исходящий SMTP (порт 587) — прямой SmtpClient до
        // smtp.gmail.com с прода никогда не подключается (таймаут, не ошибка
        // конфигурации). Локально SMTP работает нормально, поэтому в Development
        // оставлен SmtpEmailSender как есть; в остальных окружениях (Production
        // на Railway) — Resend через HTTPS API, egress-блокировка на него не
        // распространяется.
        if (isDevelopment)
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });
        }

        return services;
    }
}
