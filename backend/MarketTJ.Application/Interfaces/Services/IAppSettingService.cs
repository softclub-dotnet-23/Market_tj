using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.AppSettingDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAppSettingService
{
    Task<Result<IEnumerable<GetAppSettingDto>>> GetAllAsync();
    Task<Result<GetAppSettingDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateAppSettingDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateAppSettingDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
