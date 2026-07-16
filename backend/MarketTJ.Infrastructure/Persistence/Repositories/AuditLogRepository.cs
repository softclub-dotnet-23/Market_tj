using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class AuditLogRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task<List<AuditLog>> GetAllAsync()
        => await context.AuditLogs.ToListAsync();

    public async Task<AuditLog?> GetByIdAsync(int id)
        => await context.AuditLogs.FindAsync(id);

    public async Task AddAsync(AuditLog auditLog)
    {
        await context.AuditLogs.AddAsync(auditLog);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AuditLog auditLog)
    {
        context.AuditLogs.Update(auditLog);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(AuditLog auditLog)
    {
        context.AuditLogs.Remove(auditLog);
        await context.SaveChangesAsync();
    }
}
