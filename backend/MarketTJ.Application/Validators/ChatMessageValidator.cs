using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ChatMessageDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class ChatMessageValidator
{
    public static Result<string>? ValidateCreate(CreateChatMessageDto dto)
        => Validate(dto.ConversationId, dto.SenderId, dto.Message);

    public static Result<string>? ValidateUpdate(UpdateChatMessageDto dto)
        => Validate(dto.ConversationId, dto.SenderId, dto.Message);

    private static Result<string>? Validate(int conversationId, int senderId, string message)
    {
        if (conversationId <= 0)
            return Result<string>.Fail("ConversationId обязателен", ErrorType.Validation);

        if (senderId <= 0)
            return Result<string>.Fail("SenderId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(message))
            return Result<string>.Fail("Message обязателен", ErrorType.Validation);

        return null;
    }
}
