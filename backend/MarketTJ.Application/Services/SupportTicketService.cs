using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.SupportTicketDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class SupportTicketService(
    ISupportTicketRepository supportTicketRepository,
    IUserRepository userRepository,
    ILogger<SupportTicketService> logger) : ISupportTicketService
{
    public async Task<Result<IEnumerable<GetSupportTicketDto>>> GetAllAsync()
    {
        try
        {
            var tickets = await supportTicketRepository.GetAllAsync();
            return Result<IEnumerable<GetSupportTicketDto>>.Ok(tickets.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка тикетов поддержки");
            return Result<IEnumerable<GetSupportTicketDto>>.Fail("Не удалось получить список тикетов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetSupportTicketDto?>> GetByIdAsync(int id)
    {
        try
        {
            var ticket = await supportTicketRepository.GetByIdAsync(id);
            if (ticket is null)
                return Result<GetSupportTicketDto?>.Fail("Тикет не найден", ErrorType.NotFound);

            return Result<GetSupportTicketDto?>.Ok(ToGetDto(ticket));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении тикета {Id}", id);
            return Result<GetSupportTicketDto?>.Fail("Не удалось получить тикет", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateSupportTicketDto dto)
    {
        try
        {
            var validation = SupportTicketValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (dto.AssignedToAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.AssignedToAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("AssignedToAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            var ticket = new SupportTicket
            {
                UserId = dto.UserId,
                Subject = dto.Subject,
                Status = dto.Status,
                Priority = dto.Priority,
                ClosedAt = dto.ClosedAt,
                AssignedToAdminId = dto.AssignedToAdminId,
                CreatedAt = DateTime.UtcNow
            };

            await supportTicketRepository.AddAsync(ticket);
            return Result<string>.Ok("Тикет создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании тикета");
            return Result<string>.Fail("Не удалось создать тикет", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateSupportTicketDto dto)
    {
        try
        {
            var validation = SupportTicketValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var ticket = await supportTicketRepository.GetByIdAsync(id);
            if (ticket is null)
                return Result<string>.Fail("Тикет не найден", ErrorType.NotFound);

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            if (dto.AssignedToAdminId is not null)
            {
                var admin = await userRepository.GetByIdAsync(dto.AssignedToAdminId.Value);
                if (admin is null)
                    return Result<string>.Fail("Администратор не найден", ErrorType.NotFound);

                if (admin.Role != UserRole.Admin)
                    return Result<string>.Fail("AssignedToAdminId должен ссылаться на пользователя с ролью Admin", ErrorType.Validation);
            }

            ticket.UserId = dto.UserId;
            ticket.Subject = dto.Subject;
            ticket.Status = dto.Status;
            ticket.Priority = dto.Priority;
            ticket.ClosedAt = dto.ClosedAt;
            ticket.AssignedToAdminId = dto.AssignedToAdminId;

            await supportTicketRepository.UpdateAsync(ticket);
            return Result<string>.Ok("Тикет обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении тикета {Id}", id);
            return Result<string>.Fail("Не удалось обновить тикет", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var ticket = await supportTicketRepository.GetByIdAsync(id);
            if (ticket is null)
                return Result<string>.Fail("Тикет не найден", ErrorType.NotFound);

            await supportTicketRepository.DeleteAsync(ticket);
            return Result<string>.Ok("Тикет удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении тикета {Id}", id);
            return Result<string>.Fail("Не удалось удалить тикет", ErrorType.InternalServerError);
        }
    }

    private static GetSupportTicketDto ToGetDto(SupportTicket ticket) => new()
    {
        Id = ticket.Id,
        UserId = ticket.UserId,
        Subject = ticket.Subject,
        Status = ticket.Status,
        Priority = ticket.Priority,
        CreatedAt = ticket.CreatedAt,
        ClosedAt = ticket.ClosedAt,
        AssignedToAdminId = ticket.AssignedToAdminId
    };
}
