namespace MarketTJ.Application.Dto.AnalyticsDto;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalFarmers { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalCouriers { get; set; }

    public int TotalOrders { get; set; }
    public int OrdersToday { get; set; }
    public int OrdersThisMonth { get; set; }

    // Раздел 10.4 ТЗ: считается только по заказам в статусе Completed.
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }

    public int TotalProductListings { get; set; }
    public int ActiveProductListings { get; set; }

    public List<TopSellingProductDto> TopSellingProducts { get; set; } = [];
    public List<OrderStatusCountDto> OrdersByStatus { get; set; } = [];
    public List<MonthlyRevenueDto> RevenueByMonth { get; set; } = [];
}
