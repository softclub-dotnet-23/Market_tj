namespace MarketTJ.Application.Dto.DeliverySlotDto;

public class UpdateDeliverySlotDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime Date { get; set; }
    public string TimeFrom { get; set; } = null!;
    public string TimeTo { get; set; } = null!;
}
