using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.ChatMessageDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IChatMessageService
{
    Task<Result<IEnumerable<GetChatMessageDto>>> GetAllAsync();
    Task<Result<GetChatMessageDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateChatMessageDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateChatMessageDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
