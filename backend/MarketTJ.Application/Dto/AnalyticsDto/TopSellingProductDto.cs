namespace MarketTJ.Application.Dto.AnalyticsDto;

public class TopSellingProductDto
{
    public string ProductName { get; set; } = null!;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
