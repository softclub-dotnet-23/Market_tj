using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.NotificationDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class NotificationValidator
{
    public static Result<string>? ValidateCreate(CreateNotificationDto dto)
        => Validate(dto.UserId, dto.Title, dto.Message);

    public static Result<string>? ValidateUpdate(UpdateNotificationDto dto)
        => Validate(dto.UserId, dto.Title, dto.Message);

    private static Result<string>? Validate(int userId, string title, string message)
    {
        if (userId <= 0)
            return Result<string>.Fail("UserId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(title))
            return Result<string>.Fail("Title обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(message))
            return Result<string>.Fail("Message обязателен", ErrorType.Validation);

        return null;
    }
}
