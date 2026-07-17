namespace MarketTJ.Application.Dto.CartItemDto;

public class UpdateCartItemDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductListingId { get; set; }
    public decimal Quantity { get; set; }
}
