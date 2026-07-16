using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class SupportTicketRepository(AppDbContext context) : ISupportTicketRepository
{
    public async Task<List<SupportTicket>> GetAllAsync()
        => await context.SupportTickets.ToListAsync();

    public async Task<SupportTicket?> GetByIdAsync(int id)
        => await context.SupportTickets.FindAsync(id);

    public async Task AddAsync(SupportTicket supportTicket)
    {
        await context.SupportTickets.AddAsync(supportTicket);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SupportTicket supportTicket)
    {
        context.SupportTickets.Update(supportTicket);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(SupportTicket supportTicket)
    {
        context.SupportTickets.Remove(supportTicket);
        await context.SaveChangesAsync();
    }
}
