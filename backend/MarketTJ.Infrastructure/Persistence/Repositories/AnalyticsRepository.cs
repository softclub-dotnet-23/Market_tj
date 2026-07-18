using MarketTJ.Application.Dto.AnalyticsDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class AnalyticsRepository(AppDbContext context) : IAnalyticsRepository
{
    // Сколько последних месяцев показывать на графике RevenueByMonth
    // (раздел промпта допускает 6–12, выбрано 6 как разумный дефолт).
    private const int RevenueByMonthCount = 6;

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);

        var totalUsers = await context.Users.CountAsync();
        var totalFarmers = await context.Users.CountAsync(u => u.Role == UserRole.Farmer);
        var totalCustomers = await context.Users.CountAsync(u => u.Role == UserRole.Customer);
        var totalCouriers = await context.Users.CountAsync(u => u.Role == UserRole.Courier);

        var totalOrders = await context.Orders.CountAsync();
        var ordersToday = await context.Orders.CountAsync(o => o.CreatedAt >= todayStart && o.CreatedAt < tomorrowStart);
        var ordersThisMonth = await context.Orders.CountAsync(o => o.CreatedAt >= monthStart);

        // Раздел 10.4 ТЗ: "успешный" заказ = статус Completed. Выручка месяца
        // считается по дате фактического завершения (CompletedAt), а не по
        // дате создания заказа — так деньги попадают в тот месяц, когда они
        // реально были заработаны.
        var totalRevenue = await context.Orders
            .Where(o => o.Status == OrderStatus.Completed)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var revenueThisMonth = await context.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt != null && o.CompletedAt >= monthStart)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var totalProductListings = await context.ProductListings.CountAsync();
        var activeProductListings = await context.ProductListings.CountAsync(p => p.Status == ListingStatus.Active);

        var topSellingProducts = await context.OrderItems
            .Where(oi => oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => oi.ProductName)
            .Select(g => new TopSellingProductDto
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToListAsync();

        var ordersByStatus = await context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var revenueByMonth = await GetRevenueByMonthAsync(context.Orders.Where(o => o.Status == OrderStatus.Completed), now);

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalFarmers = totalFarmers,
            TotalCustomers = totalCustomers,
            TotalCouriers = totalCouriers,
            TotalOrders = totalOrders,
            OrdersToday = ordersToday,
            OrdersThisMonth = ordersThisMonth,
            TotalRevenue = totalRevenue,
            RevenueThisMonth = revenueThisMonth,
            TotalProductListings = totalProductListings,
            ActiveProductListings = activeProductListings,
            TopSellingProducts = topSellingProducts,
            OrdersByStatus = ordersByStatus,
            RevenueByMonth = revenueByMonth
        };
    }

    public async Task<FarmerDashboardDto> GetFarmerDashboardAsync(int farmerProfileId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalOwnProducts = await context.ProductListings.CountAsync(p => p.FarmerProfileId == farmerProfileId);
        var activeProducts = await context.ProductListings.CountAsync(p => p.FarmerProfileId == farmerProfileId && p.Status == ListingStatus.Active);

        var totalOrdersReceived = await context.Orders.CountAsync(o => o.FarmerId == farmerProfileId);
        var ordersThisMonth = await context.Orders.CountAsync(o => o.FarmerId == farmerProfileId && o.CreatedAt >= monthStart);

        var totalRevenue = await context.Orders
            .Where(o => o.FarmerId == farmerProfileId && o.Status == OrderStatus.Completed)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var revenueThisMonth = await context.Orders
            .Where(o => o.FarmerId == farmerProfileId && o.Status == OrderStatus.Completed && o.CompletedAt != null && o.CompletedAt >= monthStart)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var topSellingOwnProducts = await context.OrderItems
            .Where(oi => oi.Order.FarmerId == farmerProfileId && oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => oi.ProductName)
            .Select(g => new TopSellingProductDto
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToListAsync();

        var revenueByMonth = await GetRevenueByMonthAsync(
            context.Orders.Where(o => o.FarmerId == farmerProfileId && o.Status == OrderStatus.Completed), now);

        // Раздел 8.13 ТЗ: Review.FarmerId — FK на FarmerProfile.
        var averageRating = await context.Reviews
            .Where(r => r.FarmerId == farmerProfileId)
            .Select(r => (double?)r.Rating)
            .AverageAsync();

        return new FarmerDashboardDto
        {
            TotalOwnProducts = totalOwnProducts,
            ActiveProducts = activeProducts,
            TotalOrdersReceived = totalOrdersReceived,
            OrdersThisMonth = ordersThisMonth,
            TotalRevenue = totalRevenue,
            RevenueThisMonth = revenueThisMonth,
            TopSellingOwnProducts = topSellingOwnProducts,
            RevenueByMonth = revenueByMonth,
            AverageRating = averageRating
        };
    }

    // Группирует уже отфильтрованные (по статусу/фермеру) заказы по
    // году+месяцу CompletedAt и дополняет результат нулями для месяцев без
    // завершённых заказов — чтобы фронтенд получил непрерывный ряд для графика.
    private static async Task<List<MonthlyRevenueDto>> GetRevenueByMonthAsync(IQueryable<Domain.Entities.Order> completedOrders, DateTime now)
    {
        var earliestMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(RevenueByMonthCount - 1));

        var grouped = await completedOrders
            .Where(o => o.CompletedAt != null && o.CompletedAt >= earliestMonthStart)
            .GroupBy(o => new { o.CompletedAt!.Value.Year, o.CompletedAt.Value.Month })
            .Select(g => new MonthlyRevenueDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync();

        var result = new List<MonthlyRevenueDto>();
        for (var i = RevenueByMonthCount - 1; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var existing = grouped.FirstOrDefault(g => g.Year == monthDate.Year && g.Month == monthDate.Month);
            result.Add(existing ?? new MonthlyRevenueDto { Year = monthDate.Year, Month = monthDate.Month, Revenue = 0m });
        }

        return result;
    }
}
