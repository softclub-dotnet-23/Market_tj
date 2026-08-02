namespace MarketTJ.Application.Dto.DeliveryDto;

// Назначение/замена курьера на заказ — Admin (см. DeliveryService.AssignCourierAsync).
public class AssignCourierDto
{
    public int CourierId { get; set; }
    public decimal DeliveryFee { get; set; }
    public DateTime? EstimatedPickupAt { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public string? AdminNote { get; set; }
}
