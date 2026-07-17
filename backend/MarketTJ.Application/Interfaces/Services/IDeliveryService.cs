using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.DeliveryDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IDeliveryService
{
    Task<Result<IEnumerable<GetDeliveryDto>>> GetAllAsync();
    Task<Result<GetDeliveryDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateDeliveryDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateDeliveryDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
