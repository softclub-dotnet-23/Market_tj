namespace MarketTJ.Application.Dto.DailySalesSnapshotDto;

public class GetDailySalesSnapshotDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public int NewFarmers { get; set; }
    public int NewCustomers { get; set; }
    public int CompletedDeliveries { get; set; }
    public DateTime CreatedAt { get; set; }
}
