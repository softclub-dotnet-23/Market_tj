using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ConversationDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class ConversationValidator
{
    public static Result<string>? ValidateCreate(CreateConversationDto dto)
        => Validate(dto.OrderId, dto.CustomerId, dto.FarmerId);

    public static Result<string>? ValidateUpdate(UpdateConversationDto dto)
        => Validate(dto.OrderId, dto.CustomerId, dto.FarmerId);

    private static Result<string>? Validate(int orderId, int customerId, int farmerId)
    {
        if (orderId <= 0)
            return Result<string>.Fail("OrderId обязателен", ErrorType.Validation);

        if (customerId <= 0)
            return Result<string>.Fail("CustomerId обязателен", ErrorType.Validation);

        if (farmerId <= 0)
            return Result<string>.Fail("FarmerId обязателен", ErrorType.Validation);

        return null;
    }
}
