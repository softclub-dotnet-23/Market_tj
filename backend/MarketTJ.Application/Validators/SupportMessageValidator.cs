using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.SupportMessageDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class SupportMessageValidator
{
    public static Result<string>? ValidateCreate(CreateSupportMessageDto dto)
        => Validate(dto.SupportTicketId, dto.SenderId, dto.Message);

    public static Result<string>? ValidateUpdate(UpdateSupportMessageDto dto)
        => Validate(dto.SupportTicketId, dto.SenderId, dto.Message);

    private static Result<string>? Validate(int supportTicketId, int senderId, string message)
    {
        if (supportTicketId <= 0)
            return Result<string>.Fail("SupportTicketId обязателен", ErrorType.Validation);

        if (senderId <= 0)
            return Result<string>.Fail("SenderId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(message))
            return Result<string>.Fail("Message обязателен", ErrorType.Validation);

        return null;
    }
}
