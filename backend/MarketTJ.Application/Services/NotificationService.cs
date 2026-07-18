using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.NotificationDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<Result<IEnumerable<GetNotificationDto>>> GetAllAsync()
    {
        try
        {
            var notifications = await notificationRepository.GetAllAsync();
            return Result<IEnumerable<GetNotificationDto>>.Ok(notifications.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка уведомлений");
            return Result<IEnumerable<GetNotificationDto>>.Fail("Не удалось получить список уведомлений", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetNotificationDto?>> GetByIdAsync(int id)
    {
        try
        {
            var notification = await notificationRepository.GetByIdAsync(id);
            if (notification is null)
                return Result<GetNotificationDto?>.Fail("Уведомление не найдено", ErrorType.NotFound);

            return Result<GetNotificationDto?>.Ok(ToGetDto(notification));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении уведомления {Id}", id);
            return Result<GetNotificationDto?>.Fail("Не удалось получить уведомление", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateNotificationDto dto)
    {
        try
        {
            var validation = NotificationValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            var notification = new Notification
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                IsRead = dto.IsRead,
                CreatedAt = DateTime.UtcNow
            };

            await notificationRepository.AddAsync(notification);
            return Result<string>.Ok("Уведомление создано");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании уведомления");
            return Result<string>.Fail("Не удалось создать уведомление", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateNotificationDto dto)
    {
        try
        {
            var validation = NotificationValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var notification = await notificationRepository.GetByIdAsync(id);
            if (notification is null)
                return Result<string>.Fail("Уведомление не найдено", ErrorType.NotFound);

            var user = await userRepository.GetByIdAsync(dto.UserId);
            if (user is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            notification.UserId = dto.UserId;
            notification.Title = dto.Title;
            notification.Message = dto.Message;
            notification.IsRead = dto.IsRead;

            await notificationRepository.UpdateAsync(notification);
            return Result<string>.Ok("Уведомление обновлено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении уведомления {Id}", id);
            return Result<string>.Fail("Не удалось обновить уведомление", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var notification = await notificationRepository.GetByIdAsync(id);
            if (notification is null)
                return Result<string>.Fail("Уведомление не найдено", ErrorType.NotFound);

            await notificationRepository.DeleteAsync(notification);
            return Result<string>.Ok("Уведомление удалено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении уведомления {Id}", id);
            return Result<string>.Fail("Не удалось удалить уведомление", ErrorType.InternalServerError);
        }
    }

    private static GetNotificationDto ToGetDto(Notification notification) => new()
    {
        Id = notification.Id,
        UserId = notification.UserId,
        Title = notification.Title,
        Message = notification.Message,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };
}
