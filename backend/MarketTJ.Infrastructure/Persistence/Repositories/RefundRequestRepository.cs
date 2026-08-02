using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class RefundRequestRepository(AppDbContext context) : IRefundRequestRepository
{
    public async Task<List<RefundRequest>> GetAllAsync()
        => await context.RefundRequests.AsNoTracking().ToListAsync();

    public async Task<RefundRequest?> GetByIdAsync(int id)
        => await context.RefundRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task AddAsync(RefundRequest refundRequest)
    {
        await context.RefundRequests.AddAsync(refundRequest);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RefundRequest refundRequest)
    {
        context.RefundRequests.Update(refundRequest);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(RefundRequest refundRequest)
    {
        context.RefundRequests.Remove(refundRequest);
        await context.SaveChangesAsync();
    }
}
