using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface ISupportTicketRepository
{
    Task<List<SupportTicket>> GetAllAsync();
    Task<SupportTicket?> GetByIdAsync(int id);
    Task AddAsync(SupportTicket supportTicket);
    Task UpdateAsync(SupportTicket supportTicket);
    Task DeleteAsync(SupportTicket supportTicket);
}
