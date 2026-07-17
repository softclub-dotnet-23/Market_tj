using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ConversationDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IConversationService
{
    Task<Result<IEnumerable<GetConversationDto>>> GetAllAsync();
    Task<Result<GetConversationDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateConversationDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateConversationDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
