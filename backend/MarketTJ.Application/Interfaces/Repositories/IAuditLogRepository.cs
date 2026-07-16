using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAllAsync();
    Task<AuditLog?> GetByIdAsync(int id);
    Task AddAsync(AuditLog auditLog);
    Task UpdateAsync(AuditLog auditLog);
    Task DeleteAsync(AuditLog auditLog);
}
