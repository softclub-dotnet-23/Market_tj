using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.SupportTicketDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class SupportTicketValidator
{
    public static Result<string>? ValidateCreate(CreateSupportTicketDto dto)
        => Validate(dto.UserId, dto.Subject, dto.Status, dto.Priority);

    public static Result<string>? ValidateUpdate(UpdateSupportTicketDto dto)
        => Validate(dto.UserId, dto.Subject, dto.Status, dto.Priority);

    private static Result<string>? Validate(int userId, string subject, SupportTicketStatus status, SupportPriority priority)
    {
        if (userId <= 0)
            return Result<string>.Fail("UserId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(subject))
            return Result<string>.Fail("Subject обязателен", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус тикета", ErrorType.Validation);

        if (!Enum.IsDefined(priority))
            return Result<string>.Fail("Указан несуществующий приоритет", ErrorType.Validation);

        return null;
    }
}
