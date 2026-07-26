using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ChatMessageDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class ChatMessageService(
    IChatMessageRepository chatMessageRepository,
    IConversationRepository conversationRepository,
    IUserRepository userRepository,
    IFileStorageService fileStorageService,
    ILogger<ChatMessageService> logger) : IChatMessageService
{
    public async Task<Result<IEnumerable<GetChatMessageDto>>> GetAllAsync()
    {
        try
        {
            var messages = await chatMessageRepository.GetAllAsync();
            return Result<IEnumerable<GetChatMessageDto>>.Ok(messages.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка сообщений чата");
            return Result<IEnumerable<GetChatMessageDto>>.Fail("Не удалось получить список сообщений чата", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetChatMessageDto?>> GetByIdAsync(int id)
    {
        try
        {
            var message = await chatMessageRepository.GetByIdAsync(id);
            if (message is null)
                return Result<GetChatMessageDto?>.Fail("Сообщение не найдено", ErrorType.NotFound);

            return Result<GetChatMessageDto?>.Ok(ToGetDto(message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении сообщения {Id}", id);
            return Result<GetChatMessageDto?>.Fail("Не удалось получить сообщение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateChatMessageDto dto)
    {
        try
        {
            var validation = ChatMessageValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var conversation = await conversationRepository.GetByIdAsync(dto.ConversationId);
            if (conversation is null)
                return Result<string>.Fail("Чат не найден", ErrorType.NotFound);

            // Раздел 13.11 ТЗ: доступ к чату только у CustomerId/FarmerId заказа.
            if (dto.SenderId != conversation.CustomerId && dto.SenderId != conversation.FarmerId)
                return Result<string>.Fail("Отправитель не является участником этого чата", ErrorType.Unauthorized);

            if (conversation.IsClosed)
                return Result<string>.Fail("Чат закрыт — отправка сообщений недоступна", ErrorType.Validation);

            var sender = await userRepository.GetByIdAsync(dto.SenderId);
            if (sender is null)
                return Result<string>.Fail("Отправитель не найден", ErrorType.NotFound);

            var message = new ChatMessage
            {
                ConversationId = dto.ConversationId,
                SenderId = dto.SenderId,
                Message = dto.Message,
                ImageUrl = dto.ImageUrl,
                IsRead = dto.IsRead,
                CreatedAt = DateTime.UtcNow
            };

            await chatMessageRepository.AddAsync(message);

            conversation.UpdatedAt = DateTime.UtcNow;
            await conversationRepository.UpdateAsync(conversation);

            return Result<string>.Ok("Сообщение отправлено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании сообщения чата");
            return Result<string>.Fail("Не удалось отправить сообщение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateChatMessageDto dto)
    {
        try
        {
            var validation = ChatMessageValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var message = await chatMessageRepository.GetByIdAsync(id);
            if (message is null)
                return Result<string>.Fail("Сообщение не найдено", ErrorType.NotFound);

            var conversation = await conversationRepository.GetByIdAsync(dto.ConversationId);
            if (conversation is null)
                return Result<string>.Fail("Чат не найден", ErrorType.NotFound);

            if (dto.SenderId != conversation.CustomerId && dto.SenderId != conversation.FarmerId)
                return Result<string>.Fail("Отправитель не является участником этого чата", ErrorType.Unauthorized);

            var sender = await userRepository.GetByIdAsync(dto.SenderId);
            if (sender is null)
                return Result<string>.Fail("Отправитель не найден", ErrorType.NotFound);

            message.ConversationId = dto.ConversationId;
            message.SenderId = dto.SenderId;
            message.Message = dto.Message;
            message.ImageUrl = dto.ImageUrl;
            message.IsRead = dto.IsRead;

            await chatMessageRepository.UpdateAsync(message);
            return Result<string>.Ok("Сообщение обновлено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении сообщения {Id}", id);
            return Result<string>.Fail("Не удалось обновить сообщение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var message = await chatMessageRepository.GetByIdAsync(id);
            if (message is null)
                return Result<string>.Fail("Сообщение не найдено", ErrorType.NotFound);

            await chatMessageRepository.DeleteAsync(message);
            return Result<string>.Ok("Сообщение удалено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении сообщения {Id}", id);
            return Result<string>.Fail("Не удалось удалить сообщение", ErrorType.InternalServerError);
        }
    }

    // Фото к сообщению — тот же приём, что и ProductImageService.UploadAsync
    // (валидация файла отдельная от ChatMessageValidator.ValidateCreate,
    // потому что здесь Message не обязателен — можно отправить одно фото
    // без подписи).
    public async Task<Result<GetChatMessageDto>> UploadAsync(int conversationId, int senderId, string? caption, Stream fileContent, string fileName, long fileSizeBytes)
    {
        try
        {
            var fileValidation = FileUploadValidator.ValidateImage(fileName, fileSizeBytes);
            if (fileValidation is not null)
                return Result<GetChatMessageDto>.Fail(fileValidation.Error!, fileValidation.ErrorType!.Value);

            var conversation = await conversationRepository.GetByIdAsync(conversationId);
            if (conversation is null)
                return Result<GetChatMessageDto>.Fail("Чат не найден", ErrorType.NotFound);

            if (senderId != conversation.CustomerId && senderId != conversation.FarmerId)
                return Result<GetChatMessageDto>.Fail("Отправитель не является участником этого чата", ErrorType.Unauthorized);

            if (conversation.IsClosed)
                return Result<GetChatMessageDto>.Fail("Чат закрыт — отправка сообщений недоступна", ErrorType.Validation);

            var sender = await userRepository.GetByIdAsync(senderId);
            if (sender is null)
                return Result<GetChatMessageDto>.Fail("Отправитель не найден", ErrorType.NotFound);

            var imageUrl = await fileStorageService.SaveAsync(fileContent, fileName, $"chat/{conversationId}");

            var message = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Message = caption?.Trim() ?? "",
                ImageUrl = imageUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await chatMessageRepository.AddAsync(message);

            conversation.UpdatedAt = DateTime.UtcNow;
            await conversationRepository.UpdateAsync(conversation);

            return Result<GetChatMessageDto>.Ok(ToGetDto(message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке фото для чата {ConversationId}", conversationId);
            return Result<GetChatMessageDto>.Fail("Не удалось отправить фото", ErrorType.InternalServerError);
        }
    }

    private static GetChatMessageDto ToGetDto(ChatMessage message) => new()
    {
        Id = message.Id,
        ConversationId = message.ConversationId,
        SenderId = message.SenderId,
        Message = message.Message,
        ImageUrl = message.ImageUrl,
        IsRead = message.IsRead,
        CreatedAt = message.CreatedAt
    };
}
