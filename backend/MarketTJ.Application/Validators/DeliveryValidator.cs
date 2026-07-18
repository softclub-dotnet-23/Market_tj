using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliveryDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class DeliveryValidator
{
    public static Result<string>? ValidateCreate(CreateDeliveryDto dto)
        => Validate(dto.OrderId, dto.PickupAddress, dto.DeliveryAddress, dto.DeliveryPrice, dto.Status);

    public static Result<string>? ValidateUpdate(UpdateDeliveryDto dto)
        => Validate(dto.OrderId, dto.PickupAddress, dto.DeliveryAddress, dto.DeliveryPrice, dto.Status);

    private static Result<string>? Validate(int orderId, string pickupAddress, string deliveryAddress, decimal deliveryPrice, DeliveryStatus status)
    {
        if (orderId <= 0)
            return Result<string>.Fail("OrderId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(pickupAddress))
            return Result<string>.Fail("PickupAddress обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(deliveryAddress))
            return Result<string>.Fail("DeliveryAddress обязателен", ErrorType.Validation);

        if (deliveryPrice < 0)
            return Result<string>.Fail("DeliveryPrice не может быть отрицательной", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус доставки", ErrorType.Validation);

        return null;
    }
}
