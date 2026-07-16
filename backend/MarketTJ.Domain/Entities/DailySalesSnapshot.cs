namespace MarketTJ.Domain.Entities;

// Агрегат для исторических графиков (раздел 8.30) — заполняется фоновой
// задачей, не имеет связей с другими сущностями.
public class DailySalesSnapshot
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
