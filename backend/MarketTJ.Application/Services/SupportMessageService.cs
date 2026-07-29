using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.SupportMessageDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class SupportMessageService(
    ISupportMessageRepository supportMessageRepository,
    ISupportTicketRepository supportTicketRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    ILogger<SupportMessageService> logger) : ISupportMessageService
{
    // Audit 2026-07-28, находка 2.2 (IDOR): "моё" здесь — я автор тикета или
    // назначенный на него админ (или любой Admin), а не только "я отправитель"
    // (иначе клиент не увидел бы ответы поддержки).
    private async Task<bool> CanAccessTicketAsync(int ticketId)
    {
        if (currentUser.IsAdmin())
            return true;

        var ticket = await supportTicketRepository.GetByIdAsync(ticketId);
        return ticket is not null && ticket.UserId == currentUser.UserId;
    }

    public async Task<Result<IEnumerable<GetSupportMessageDto>>> GetAllAsync()
    {
        try
        {
            var messages = await supportMessageRepository.GetAllAsync();

            if (!currentUser.IsAdmin())
            {
                var tickets = await supportTicketRepository.GetAllAsync();
                var myTicketIds = tickets.Where(t => t.UserId == currentUser.UserId).Select(t => t.Id).ToHashSet();
                messages = messages.Where(m => myTicketIds.Contains(m.SupportTicketId)).ToList();
            }

            return Result<IEnumerable<GetSupportMessageDto>>.Ok(messages.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка сообщений поддержки");
            return Result<IEnumerable<GetSupportMessageDto>>.Fail("Не удалось получить список сообщений поддержки", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetSupportMessageDto?>> GetByIdAsync(int id)
    {
        try
        {
            var message = await supportMessageRepository.GetByIdAsync(id);
            if (message is null)
                return Result<GetSupportMessageDto?>.Fail("Сообщение не найдено", ErrorType.NotFound);

            if (!await CanAccessTicketAsync(message.SupportTicketId))
                return Result<GetSupportMessageDto?>.Fail("Нет доступа к этому сообщению", ErrorType.Forbidden);

            return Result<GetSupportMessageDto?>.Ok(ToGetDto(message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении сообщения {Id}", id);
            return Result<GetSupportMessageDto?>.Fail("Не удалось получить сообщение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateSupportMessageDto dto)
    {
        try
        {
            var validation = SupportMessageValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var ticket = await supportTicketRepository.GetByIdAsync(dto.SupportTicketId);
            if (ticket is null)
                return Result<string>.Fail("Тикет не найден", ErrorType.NotFound);

            // SenderId из тела не используется — отправитель всегда текущий
            // пользователь (audit 2026-07-28, находка 2.2, тот же паттерн, что
            // и в ChatMessageService).
            var senderId = currentUser.UserId ?? dto.SenderId;
            if (!currentUser.IsAdmin() && ticket.UserId != senderId)
                return Result<string>.Fail("Нет доступа к этому тикету", ErrorType.Forbidden);

            var sender = await userRepository.GetByIdAsync(senderId);
            if (sender is null)
                return Result<string>.Fail("Отправитель не найден", ErrorType.NotFound);

            var message = new SupportMessage
            {
                SupportTicketId = dto.SupportTicketId,
                SenderId = senderId,
                Message = dto.Message,
                CreatedAt = DateTime.UtcNow
            };

            await supportMessageRepository.AddAsync(message);
            return Result<string>.Ok("Сообщение отправлено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании сообщения поддержки");
            return Result<string>.Fail("Не удалось отправить сообщение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateSupportMessageDto dto)
    {
        try
        {
            var validation = SupportMessageValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var message = await supportMessageRepository.GetByIdAsync(id);
            if (message is null)
                return Result<string>.Fail("Сообщение не найдено", ErrorType.NotFound);

            if (!await CanAccessTicketAsync(message.SupportTicketId))
                return Result<string>.Fail("Нет доступа к этому сообщению", ErrorType.Forbidden);

            var ticket = await supportTicketRepository.GetByIdAsync(dto.SupportTicketId);
            if (ticket is null)
                return Result<string>.Fail("Тикет не найден", ErrorType.NotFound);

            var sender = await userRepository.GetByIdAsync(dto.SenderId);
            if (sender is null)
                return Result<string>.Fail("Отправитель не найден", ErrorType.NotFound);

            message.Message = dto.Message;

            await supportMessageRepository.UpdateAsync(message);
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
            var message = await supportMessageRepository.GetByIdAsync(id);
            if (message is null)
                return Result<string>.Fail("Сообщение не найдено", ErrorType.NotFound);

            if (!await CanAccessTicketAsync(message.SupportTicketId))
                return Result<string>.Fail("Нет доступа к этому сообщению", ErrorType.Forbidden);

            await supportMessageRepository.DeleteAsync(message);
            return Result<string>.Ok("Сообщение удалено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении сообщения {Id}", id);
            return Result<string>.Fail("Не удалось удалить сообщение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<GetSupportMessageDto>>> GetByTicketIdAsync(int ticketId)
    {
        try
        {
            var ticket = await supportTicketRepository.GetByIdAsync(ticketId);
            if (ticket is null)
                return Result<IEnumerable<GetSupportMessageDto>>.Fail("Тикет не найден", ErrorType.NotFound);

            if (!currentUser.IsAdmin() && ticket.UserId != currentUser.UserId)
                return Result<IEnumerable<GetSupportMessageDto>>.Fail("Нет доступа к этому тикету", ErrorType.Forbidden);

            var messages = await supportMessageRepository.GetAllAsync();
            var ordered = messages
                .Where(m => m.SupportTicketId == ticketId)
                .OrderBy(m => m.CreatedAt)
                .Select(ToGetDto);

            return Result<IEnumerable<GetSupportMessageDto>>.Ok(ordered);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении сообщений тикета {TicketId}", ticketId);
            return Result<IEnumerable<GetSupportMessageDto>>.Fail("Не удалось получить сообщения тикета", ErrorType.InternalServerError);
        }
    }

    private static GetSupportMessageDto ToGetDto(SupportMessage message) => new()
    {
        Id = message.Id,
        SupportTicketId = message.SupportTicketId,
        SenderId = message.SenderId,
        Message = message.Message,
        CreatedAt = message.CreatedAt
    };
}
