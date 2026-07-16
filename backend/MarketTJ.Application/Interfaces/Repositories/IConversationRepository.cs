using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IConversationRepository
{
    Task<List<Conversation>> GetAllAsync();
    Task<Conversation?> GetByIdAsync(int id);
    Task AddAsync(Conversation conversation);
    Task UpdateAsync(Conversation conversation);
    Task DeleteAsync(Conversation conversation);
}
