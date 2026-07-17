using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.DeliveryDto;

public class UpdateDeliveryDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? CourierId { get; set; }
    public string PickupAddress { get; set; } = null!;
    public string DeliveryAddress { get; set; } = null!;
    public decimal DeliveryPrice { get; set; }
    public DeliveryStatus Status { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
