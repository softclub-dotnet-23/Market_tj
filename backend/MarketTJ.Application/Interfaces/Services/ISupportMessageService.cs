using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.SupportMessageDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ISupportMessageService
{
    Task<Result<IEnumerable<GetSupportMessageDto>>> GetAllAsync();
    Task<Result<GetSupportMessageDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateSupportMessageDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateSupportMessageDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
