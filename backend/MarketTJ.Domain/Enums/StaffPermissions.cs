namespace MarketTJ.Domain.Enums;

[Flags]
public enum StaffPermissions
{
    None = 0,
    ManageProducts = 1,
    ManageStock = 2
}
