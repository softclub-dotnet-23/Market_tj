namespace MarketTJ.Application.Dto.DeliveryZoneDto;

public class GetDeliveryZoneDto
{
    public int Id { get; set; }
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public decimal? PricePerKm { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
