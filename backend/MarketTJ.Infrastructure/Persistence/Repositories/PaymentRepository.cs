using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class PaymentRepository(AppDbContext context) : IPaymentRepository
{
    public async Task<List<Payment>> GetAllAsync()
        => await context.Payments.ToListAsync();

    public async Task<Payment?> GetByIdAsync(int id)
        => await context.Payments.FindAsync(id);

    public async Task AddAsync(Payment payment)
    {
        await context.Payments.AddAsync(payment);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        context.Payments.Update(payment);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Payment payment)
    {
        context.Payments.Remove(payment);
        await context.SaveChangesAsync();
    }
}
