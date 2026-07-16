using MarketTJ.Domain.Entities;

namespace MarketTJ.Application.Interfaces.Repositories;

public interface IFarmerStaffMemberRepository
{
    Task<List<FarmerStaffMember>> GetAllAsync();
    Task<FarmerStaffMember?> GetByIdAsync(int id);
    Task AddAsync(FarmerStaffMember farmerStaffMember);
    Task UpdateAsync(FarmerStaffMember farmerStaffMember);
    Task DeleteAsync(FarmerStaffMember farmerStaffMember);
}
