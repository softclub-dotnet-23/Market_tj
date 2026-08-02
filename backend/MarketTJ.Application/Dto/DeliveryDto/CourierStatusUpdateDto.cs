using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.DeliveryDto;

// Курьер продвигает доставку по разрешённым шагам (см.
// DeliveryService.CourierTransitions) — GoingToFarmer/ArrivedAtFarmer/
// PickedUp/InTransit/ArrivedAtClient. Delivered достигается только через
// ConfirmDeliveryAsync (код подтверждения), не через этот метод.
public class CourierStatusUpdateDto
{
    public DeliveryStatus Status { get; set; }
    public string? Note { get; set; }
}
