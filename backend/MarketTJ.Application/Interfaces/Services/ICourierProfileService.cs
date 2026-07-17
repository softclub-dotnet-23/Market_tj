using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.CourierProfileDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ICourierProfileService
{
    Task<Result<IEnumerable<GetCourierProfileDto>>> GetAllAsync();
    Task<Result<GetCourierProfileDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateCourierProfileDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateCourierProfileDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
