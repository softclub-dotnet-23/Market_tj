namespace MarketTJ.Domain.Enums;

// Расширено по прямому запросу пользователя (2026-08-02) — полноценное
// назначение и отслеживание курьера. Более гранулярно, чем раньше (было
// только Pending/Assigned/PickedUp/InDelivery/Delivered/Cancelled) — курьеру
// нужны отдельные шаги Accepted/GoingToFarmer/ArrivedAtFarmer/ArrivedAtClient,
// чтобы кнопки в его интерфейсе имели точный "текущий шаг". Админ/фермер/
// покупатель видят более крупные группы этих же статусов на бейджах.
public enum DeliveryStatus
{
    Pending = 1,
    Assigned = 2,
    Accepted = 3,
    GoingToFarmer = 4,
    ArrivedAtFarmer = 5,
    PickedUp = 6,
    InTransit = 7,
    ArrivedAtClient = 8,
    Delivered = 9,
    Cancelled = 10
}
