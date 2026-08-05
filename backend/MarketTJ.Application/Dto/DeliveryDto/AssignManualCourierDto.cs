namespace MarketTJ.Application.Dto.DeliveryDto;

public class AssignManualCourierDto
{
    public string CourierName { get; set; } = null!;
    public string CourierPhone { get; set; } = null!;
    public decimal DeliveryFee { get; set; }
    public DateTime? EstimatedPickupAt { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public string? AdminNote { get; set; }
}
