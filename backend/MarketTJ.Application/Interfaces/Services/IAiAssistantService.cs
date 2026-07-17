using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IAiAssistantService
{
    Task<Result<AssistantResponseDto>> AskAsync(string message);
}
