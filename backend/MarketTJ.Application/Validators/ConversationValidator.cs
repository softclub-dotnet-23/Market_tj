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

    // OrderId необязателен (null — чат до заказа, вопрос фермеру про товар),
    // но если он передан, то должен быть положительным.
    private static Result<string>? Validate(int? orderId, int customerId, int farmerId)
    {
        if (orderId.HasValue && orderId.Value <= 0)
            return Result<string>.Fail("OrderId должен быть положительным", ErrorType.Validation);

        if (customerId <= 0)
            return Result<string>.Fail("CustomerId обязателен", ErrorType.Validation);

        if (farmerId <= 0)
            return Result<string>.Fail("FarmerId обязателен", ErrorType.Validation);

        return null;
    }
}
