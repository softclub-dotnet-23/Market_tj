# State Machines

> Источник: TZ_MarketTJ_ClaudeCode.md, раздел 7.

## Статусы фермера

```csharp
public enum FarmerVerificationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Suspended = 4
}
```

## Статусы объявления

```csharp
public enum ListingStatus
{
    Draft = 1,
    Active = 2,
    Hidden = 3,
    OutOfStock = 4,
    Blocked = 5
}
```

## Статусы заказа

```csharp
public enum OrderStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Preparing = 4,
    ReadyForPickup = 5,
    CourierAssigned = 6,
    PickedUp = 7,
    InDelivery = 8,
    Delivered = 9,
    Completed = 10,
    Cancelled = 11
}
```

## Статусы доставки

```csharp
public enum DeliveryStatus
{
    Pending = 1,
    Assigned = 2,
    PickedUp = 3,
    InDelivery = 4,
    Delivered = 5,
    Cancelled = 6
}
```

Переходы между статусами и правила, кто может их менять, — см. [DesignerLogic.md](DesignerLogic.md).

Courier (mini-interface, 2 страницы) может выставлять только 3 из статусов `DeliveryStatus`: `PickedUp`, `InDelivery`, `Delivered`. Переходы `Pending → Assigned` и `Cancelled` — исключительно на стороне Admin.
