using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IAiConversationLogRepository
{
    Task<List<AiConversationLog>> GetAllAsync();
    Task AddAsync(AiConversationLog log);
}
