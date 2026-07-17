namespace MarketTJ.Application.Dto.CartItemDto;

public class CreateCartItemDto
{
    public int CustomerId { get; set; }
    public int ProductListingId { get; set; }
    public decimal Quantity { get; set; }
}
