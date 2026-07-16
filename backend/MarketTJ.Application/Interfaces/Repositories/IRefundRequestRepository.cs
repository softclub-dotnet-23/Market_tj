using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IRefundRequestRepository
{
    Task<List<RefundRequest>> GetAllAsync();
    Task<RefundRequest?> GetByIdAsync(int id);
    Task AddAsync(RefundRequest refundRequest);
    Task UpdateAsync(RefundRequest refundRequest);
    Task DeleteAsync(RefundRequest refundRequest);
}
