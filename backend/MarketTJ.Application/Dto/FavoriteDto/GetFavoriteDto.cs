namespace MarketTJ.Application.Dto.FavoriteDto;

public class GetFavoriteDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int ProductListingId { get; set; }
    public DateTime CreatedAt { get; set; }
}
