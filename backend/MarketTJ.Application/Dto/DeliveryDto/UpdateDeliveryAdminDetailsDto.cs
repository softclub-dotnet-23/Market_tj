namespace MarketTJ.Application.Dto.DeliveryDto;

// Правка суммы/сроков/заметки уже назначенной доставки — Admin.
public class UpdateDeliveryAdminDetailsDto
{
    public decimal DeliveryFee { get; set; }
    public DateTime? EstimatedPickupAt { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public string? AdminNote { get; set; }
}
