using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.NotificationDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface INotificationService
{
    Task<Result<IEnumerable<GetNotificationDto>>> GetAllAsync();
    Task<Result<GetNotificationDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateNotificationDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateNotificationDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
