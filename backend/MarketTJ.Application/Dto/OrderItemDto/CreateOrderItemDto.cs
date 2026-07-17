namespace MarketTJ.Application.Dto.OrderItemDto;

public class CreateOrderItemDto
{
    public int OrderId { get; set; }
    public int ProductListingId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}
