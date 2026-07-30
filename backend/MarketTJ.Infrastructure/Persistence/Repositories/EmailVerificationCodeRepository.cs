using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class EmailVerificationCodeRepository(AppDbContext context) : IEmailVerificationCodeRepository
{
    public async Task<EmailVerificationCode?> GetLatestByEmailAsync(string email)
        => await context.EmailVerificationCodes
            .Where(x => x.Email == email)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task AddAsync(EmailVerificationCode code)
    {
        await context.EmailVerificationCodes.AddAsync(code);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(EmailVerificationCode code)
    {
        context.EmailVerificationCodes.Update(code);
        await context.SaveChangesAsync();
    }
}
