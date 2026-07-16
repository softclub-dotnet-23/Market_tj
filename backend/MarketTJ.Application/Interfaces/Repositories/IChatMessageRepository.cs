using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IChatMessageRepository
{
    Task<List<ChatMessage>> GetAllAsync();
    Task<ChatMessage?> GetByIdAsync(int id);
    Task AddAsync(ChatMessage chatMessage);
    Task UpdateAsync(ChatMessage chatMessage);
    Task DeleteAsync(ChatMessage chatMessage);
}
