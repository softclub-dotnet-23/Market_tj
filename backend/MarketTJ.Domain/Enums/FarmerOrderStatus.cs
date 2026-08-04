namespace MarketTJ.Domain.Enums;

// Явно разделённый от Order.Status статус, который пишет ТОЛЬКО фермер
// (через OrderService/DeliveryService.AssignCourierAsync) — по прямому
// запросу пользователя (2026-08-04), чтобы курьерские действия физически
// не могли перезаписать то, что выставил фермер. См. миграцию
// AddFarmerCourierOrderStatus для маппинга со старым Order.Status.
public enum FarmerOrderStatus
{
    Accepted = 1,
    HandedToCourier = 2
}
