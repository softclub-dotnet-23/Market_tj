using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.FarmerProfileDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IFarmerProfileService
{
    Task<Result<IEnumerable<GetFarmerProfileDto>>> GetAllAsync();
    Task<Result<GetFarmerProfileDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateFarmerProfileDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateFarmerProfileDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
