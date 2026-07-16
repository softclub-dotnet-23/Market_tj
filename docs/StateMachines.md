# State Machines

> Источник: TZ_MarketTJ_ClaudeCode.md, раздел 7.

## Статусы фермера

> Обновлено (Phase 1 Step 1): Suspended убран, Approved → Verified.

```csharp
public enum FarmerVerificationStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3
}
```

## Статусы объявления

> Обновлено (Phase 1 Step 1): Hidden убран, Blocked → Archived.

```csharp
public enum ListingStatus
{
    Draft = 1,
    Active = 2,
    OutOfStock = 3,
    Archived = 4
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
