using MarketTJ.Application.Dto.PlatformSettingsDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IPlatformSettingsService
{
    Task<Result<GetPlatformSettingsDto>> GetAsync();
    Task<Result<string>> UpdateAsync(UpdatePlatformSettingsDto dto, int adminUserId);
}
