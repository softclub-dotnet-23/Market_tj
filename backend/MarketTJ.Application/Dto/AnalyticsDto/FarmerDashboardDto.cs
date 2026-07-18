namespace MarketTJ.Application.Dto.AnalyticsDto;

public class FarmerDashboardDto
{
    public int TotalOwnProducts { get; set; }
    public int ActiveProducts { get; set; }

    public int TotalOrdersReceived { get; set; }
    public int OrdersThisMonth { get; set; }

    // Раздел 10.4 ТЗ: считается только по заказам этого фермера в статусе Completed.
    public decimal TotalRevenue { get; set; }
    public decimal RevenueThisMonth { get; set; }

    public List<TopSellingProductDto> TopSellingOwnProducts { get; set; } = [];
    public List<MonthlyRevenueDto> RevenueByMonth { get; set; } = [];

    // Null, если у фермера ещё нет ни одного отзыва.
    public double? AverageRating { get; set; }
}
