using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketTJ.Infrastructure.Persistence.Repositories;

public class FarmerStaffMemberRepository(AppDbContext context) : IFarmerStaffMemberRepository
{
    public async Task<List<FarmerStaffMember>> GetAllAsync()
        => await context.FarmerStaffMembers.ToListAsync();

    public async Task<FarmerStaffMember?> GetByIdAsync(int id)
        => await context.FarmerStaffMembers.FindAsync(id);

    public async Task AddAsync(FarmerStaffMember farmerStaffMember)
    {
        await context.FarmerStaffMembers.AddAsync(farmerStaffMember);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FarmerStaffMember farmerStaffMember)
    {
        context.FarmerStaffMembers.Update(farmerStaffMember);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(FarmerStaffMember farmerStaffMember)
    {
        context.FarmerStaffMembers.Remove(farmerStaffMember);
        await context.SaveChangesAsync();
    }
}
