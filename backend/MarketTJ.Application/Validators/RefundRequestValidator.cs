using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.RefundRequestDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class RefundRequestValidator
{
    public static Result<string>? ValidateCreate(CreateRefundRequestDto dto)
        => Validate(dto.OrderId, dto.CustomerId, dto.Reason, dto.Amount, dto.Status);

    public static Result<string>? ValidateUpdate(UpdateRefundRequestDto dto)
        => Validate(dto.OrderId, dto.CustomerId, dto.Reason, dto.Amount, dto.Status);

    private static Result<string>? Validate(int orderId, int customerId, string reason, decimal amount, RefundStatus status)
    {
        if (orderId <= 0)
            return Result<string>.Fail("OrderId обязателен", ErrorType.Validation);

        if (customerId <= 0)
            return Result<string>.Fail("CustomerId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(reason))
            return Result<string>.Fail("Reason обязателен", ErrorType.Validation);

        if (amount <= 0)
            return Result<string>.Fail("Amount должна быть больше 0", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус возврата", ErrorType.Validation);

        return null;
    }
}
