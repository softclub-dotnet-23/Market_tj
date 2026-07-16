using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface ISupportMessageRepository
{
    Task<List<SupportMessage>> GetAllAsync();
    Task<SupportMessage?> GetByIdAsync(int id);
    Task AddAsync(SupportMessage supportMessage);
    Task UpdateAsync(SupportMessage supportMessage);
    Task DeleteAsync(SupportMessage supportMessage);
}
