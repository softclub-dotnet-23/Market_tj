namespace MarketTJ.Domain.Enums;

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
