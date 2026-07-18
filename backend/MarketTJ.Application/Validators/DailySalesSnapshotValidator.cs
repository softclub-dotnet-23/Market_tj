using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DailySalesSnapshotDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class DailySalesSnapshotValidator
{
    public static Result<string>? ValidateCreate(CreateDailySalesSnapshotDto dto)
        => Validate(dto.Date, dto.TotalOrders, dto.TotalRevenue, dto.TotalCommission, dto.NewFarmers, dto.NewCustomers, dto.CompletedDeliveries);

    public static Result<string>? ValidateUpdate(UpdateDailySalesSnapshotDto dto)
        => Validate(dto.Date, dto.TotalOrders, dto.TotalRevenue, dto.TotalCommission, dto.NewFarmers, dto.NewCustomers, dto.CompletedDeliveries);

    private static Result<string>? Validate(DateTime date, int totalOrders, decimal totalRevenue, decimal totalCommission, int newFarmers, int newCustomers, int completedDeliveries)
    {
        if (date == default)
            return Result<string>.Fail("Date обязателен", ErrorType.Validation);

        if (totalOrders < 0)
            return Result<string>.Fail("TotalOrders не может быть отрицательным", ErrorType.Validation);

        if (totalRevenue < 0)
            return Result<string>.Fail("TotalRevenue не может быть отрицательной", ErrorType.Validation);

        if (totalCommission < 0)
            return Result<string>.Fail("TotalCommission не может быть отрицательной", ErrorType.Validation);

        if (newFarmers < 0)
            return Result<string>.Fail("NewFarmers не может быть отрицательным", ErrorType.Validation);

        if (newCustomers < 0)
            return Result<string>.Fail("NewCustomers не может быть отрицательным", ErrorType.Validation);

        if (completedDeliveries < 0)
            return Result<string>.Fail("CompletedDeliveries не может быть отрицательным", ErrorType.Validation);

        return null;
    }
}
