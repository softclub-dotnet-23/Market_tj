using System.Text.RegularExpressions;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliverySlotDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static partial class DeliverySlotValidator
{
    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimeRegex();

    public static Result<string>? ValidateCreate(CreateDeliverySlotDto dto)
        => Validate(dto.OrderId, dto.Date, dto.TimeFrom, dto.TimeTo);

    public static Result<string>? ValidateUpdate(UpdateDeliverySlotDto dto)
        => Validate(dto.OrderId, dto.Date, dto.TimeFrom, dto.TimeTo);

    private static Result<string>? Validate(int orderId, DateTime date, string timeFrom, string timeTo)
    {
        if (orderId <= 0)
            return Result<string>.Fail("OrderId обязателен", ErrorType.Validation);

        if (date == default)
            return Result<string>.Fail("Date обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(timeFrom) || !TimeRegex().IsMatch(timeFrom))
            return Result<string>.Fail("TimeFrom должен быть в формате HH:mm", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(timeTo) || !TimeRegex().IsMatch(timeTo))
            return Result<string>.Fail("TimeTo должен быть в формате HH:mm", ErrorType.Validation);

        if (string.CompareOrdinal(timeFrom, timeTo) >= 0)
            return Result<string>.Fail("TimeFrom должен быть раньше TimeTo", ErrorType.Validation);

        return null;
    }
}
