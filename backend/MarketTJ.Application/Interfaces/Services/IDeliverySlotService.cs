using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.DeliverySlotDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IDeliverySlotService
{
    Task<Result<IEnumerable<GetDeliverySlotDto>>> GetAllAsync();
    Task<Result<GetDeliverySlotDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateDeliverySlotDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateDeliverySlotDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
