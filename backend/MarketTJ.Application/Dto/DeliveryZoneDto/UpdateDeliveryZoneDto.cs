namespace MarketTJ.Application.Dto.DeliveryZoneDto;

public class UpdateDeliveryZoneDto
{
    public int Id { get; set; }
    public string Region { get; set; } = null!;
    public string District { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public decimal? PricePerKm { get; set; }
    public bool IsActive { get; set; }
}
