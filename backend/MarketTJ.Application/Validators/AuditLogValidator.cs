using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AuditLogDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class AuditLogValidator
{
    public static Result<string>? ValidateCreate(CreateAuditLogDto dto)
        => Validate(dto.AdminId, dto.Action, dto.EntityType, dto.EntityId, dto.Details);

    public static Result<string>? ValidateUpdate(UpdateAuditLogDto dto)
        => Validate(dto.AdminId, dto.Action, dto.EntityType, dto.EntityId, dto.Details);

    private static Result<string>? Validate(int adminId, string action, string entityType, int entityId, string details)
    {
        if (adminId <= 0)
            return Result<string>.Fail("AdminId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(action))
            return Result<string>.Fail("Action обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(entityType))
            return Result<string>.Fail("EntityType обязателен", ErrorType.Validation);

        if (entityId <= 0)
            return Result<string>.Fail("EntityId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(details))
            return Result<string>.Fail("Details обязателен", ErrorType.Validation);

        return null;
    }
}
