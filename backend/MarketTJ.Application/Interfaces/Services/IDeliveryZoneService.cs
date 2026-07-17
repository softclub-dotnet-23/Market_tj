using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.DeliveryZoneDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IDeliveryZoneService
{
    Task<Result<IEnumerable<GetDeliveryZoneDto>>> GetAllAsync();
    Task<Result<GetDeliveryZoneDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateDeliveryZoneDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateDeliveryZoneDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
