using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IOrderCancellationRepository
{
    Task AddAsync(OrderCancellation cancellation);
    Task<int> CountSinceAsync(int userId, string role, DateTime since);
}
