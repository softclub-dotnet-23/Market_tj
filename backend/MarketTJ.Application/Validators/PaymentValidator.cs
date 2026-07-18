using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.PaymentDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class PaymentValidator
{
    public static Result<string>? ValidateCreate(CreatePaymentDto dto)
        => Validate(dto.OrderId, dto.Amount, dto.Method, dto.Status);

    public static Result<string>? ValidateUpdate(UpdatePaymentDto dto)
        => Validate(dto.OrderId, dto.Amount, dto.Method, dto.Status);

    private static Result<string>? Validate(int orderId, decimal amount, PaymentMethod method, PaymentStatus status)
    {
        if (orderId <= 0)
            return Result<string>.Fail("OrderId обязателен", ErrorType.Validation);

        if (amount <= 0)
            return Result<string>.Fail("Amount должна быть больше 0", ErrorType.Validation);

        if (!Enum.IsDefined(method))
            return Result<string>.Fail("Указан несуществующий способ оплаты", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус оплаты", ErrorType.Validation);

        return null;
    }
}
