using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ChatMessageDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class ChatMessageValidator
{
    public static Result<string>? ValidateCreate(CreateChatMessageDto dto)
        => Validate(dto.ConversationId, dto.SenderId, dto.Message, dto.ImageUrl);

    public static Result<string>? ValidateUpdate(UpdateChatMessageDto dto)
        => Validate(dto.ConversationId, dto.SenderId, dto.Message, dto.ImageUrl);

    // Message обязателен, только если у сообщения нет фото — фото-сообщение
    // без подписи (Message = "") валидно, и его тоже можно редактировать/
    // помечать прочитанным через этот же путь (см. ChatMessageService.UploadAsync).
    private static Result<string>? Validate(int conversationId, int senderId, string message, string? imageUrl)
    {
        if (conversationId <= 0)
            return Result<string>.Fail("ConversationId обязателен", ErrorType.Validation);

        if (senderId <= 0)
            return Result<string>.Fail("SenderId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(imageUrl))
            return Result<string>.Fail("Message обязателен", ErrorType.Validation);

        return null;
    }
}
