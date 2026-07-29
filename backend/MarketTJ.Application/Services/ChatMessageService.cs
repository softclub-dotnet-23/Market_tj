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
    ICurrentUserService currentUser,
    ILogger<ChatMessageService> logger) : IChatMessageService
{
    // Audit 2026-07-28, находка 2.2/3.8 (IDOR): участник чата — не тот, кто это
    // заявил телом запроса, а тот, кто реально залогинен (JWT).
    private bool IsParticipant(Conversation conversation)
        => currentUser.IsAdmin() || conversation.CustomerId == currentUser.UserId || conversation.FarmerId == currentUser.UserId;

    public async Task<Result<IEnumerable<GetChatMessageDto>>> GetAllAsync()
    {
        try
        {
            var messages = await chatMessageRepository.GetAllAsync();

            // "Моё" здесь — не "я отправитель", а "я участник переписки":
            // иначе Customer не увидел бы входящие сообщения от Farmer.
            if (!currentUser.IsAdmin())
            {
                var conversations = await conversationRepository.GetAllAsync();
                var myConversationIds = conversations
                    .Where(c => c.CustomerId == currentUser.UserId || c.FarmerId == currentUser.UserId)
                    .Select(c => c.Id)
                    .ToHashSet();
                messages = messages.Where(m => myConversationIds.Contains(m.ConversationId)).ToList();
            }

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

            var conversation = await conversationRepository.GetByIdAsync(message.ConversationId);
            if (conversation is null || !IsParticipant(conversation))
                return Result<GetChatMessageDto?>.Fail("Нет доступа к этому сообщению", ErrorType.Forbidden);

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

            // SenderId из тела запроса не используется — отправитель всегда
            // текущий пользователь (audit 2026-07-28, находка 3.8: раньше
            // dto.SenderId позволял слать сообщения от имени другого участника).
            var senderId = currentUser.UserId ?? dto.SenderId;
            if (!IsParticipant(conversation))
                return Result<string>.Fail("Отправитель не является участником этого чата", ErrorType.Forbidden);

            if (conversation.IsClosed)
                return Result<string>.Fail("Чат закрыт — отправка сообщений недоступна", ErrorType.Validation);

            var sender = await userRepository.GetByIdAsync(senderId);
            if (sender is null)
                return Result<string>.Fail("Отправитель не найден", ErrorType.NotFound);

            var message = new ChatMessage
            {
                ConversationId = dto.ConversationId,
                SenderId = senderId,
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

            // IDOR-guard: редактировать/помечать прочитанным можно только своё
            // сообщение (или будучи участником чата — IsRead ставит получатель).
            var conversation = await conversationRepository.GetByIdAsync(message.ConversationId);
            if (conversation is null || !IsParticipant(conversation))
                return Result<string>.Fail("Нет доступа к этому сообщению", ErrorType.Forbidden);

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

            var conversation = await conversationRepository.GetByIdAsync(message.ConversationId);
            if (conversation is null || !IsParticipant(conversation))
                return Result<string>.Fail("Нет доступа к этому сообщению", ErrorType.Forbidden);

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
            // audit 2026-07-28, находка 3.8: senderId-параметр (пришедший из формы
            // запроса через контроллер) не используется для владения — только
            // текущий пользователь может быть отправителем.
            senderId = currentUser.UserId ?? senderId;

            var fileValidation = FileUploadValidator.ValidateImage(fileName, fileSizeBytes);
            if (fileValidation is not null)
                return Result<GetChatMessageDto>.Fail(fileValidation.Error!, fileValidation.ErrorType!.Value);

            var conversation = await conversationRepository.GetByIdAsync(conversationId);
            if (conversation is null)
                return Result<GetChatMessageDto>.Fail("Чат не найден", ErrorType.NotFound);

            if (!IsParticipant(conversation))
                return Result<GetChatMessageDto>.Fail("Отправитель не является участником этого чата", ErrorType.Forbidden);

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
