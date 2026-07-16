using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<FarmerProfile> FarmerProfiles => Set<FarmerProfile>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<CourierProfile> CourierProfiles => Set<CourierProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductListing> ProductListings => Set<ProductListing>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<FarmerDocument> FarmerDocuments => Set<FarmerDocument>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ReportedListing> ReportedListings => Set<ReportedListing>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<FarmerStaffMember> FarmerStaffMembers => Set<FarmerStaffMember>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<DeliverySlot> DeliverySlots => Set<DeliverySlot>();
    public DbSet<DailySalesSnapshot> DailySalesSnapshots => Set<DailySalesSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Подхватывает все IEntityTypeConfiguration<T> из этой сборки
        // (backend/MarketTJ.Infrastructure/Persistence/Configurations/).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
