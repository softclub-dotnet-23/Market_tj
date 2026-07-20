using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.AnalyticsDto;

public class OrderStatusCountDto
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
}
