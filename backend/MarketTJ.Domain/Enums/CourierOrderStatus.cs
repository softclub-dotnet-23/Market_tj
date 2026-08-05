namespace MarketTJ.Domain.Enums;

// Явно разделённый от Order.Status статус, который пишет ТОЛЬКО курьер
// (через DeliveryService.AcceptAsync/ConfirmDeliveryAsync) — см. комментарий
// в FarmerOrderStatus.cs. Намеренно грубее, чем DeliveryStatus (10 шагов) —
// курьеру для мелких шагов (GoingToFarmer/ArrivedAtFarmer/PickedUp/InTransit/
// ArrivedAtClient) хватает Delivery.Status, здесь только 2 значения,
// которые видит фермер/покупатель: "принял" и "доставлено".
public enum CourierOrderStatus
{
    Accepted = 1,
    Delivered = 2
}
