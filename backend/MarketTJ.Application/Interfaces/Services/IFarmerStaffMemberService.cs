using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.FarmerStaffMemberDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IFarmerStaffMemberService
{
    Task<Result<IEnumerable<GetFarmerStaffMemberDto>>> GetAllAsync();
    Task<Result<GetFarmerStaffMemberDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateFarmerStaffMemberDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateFarmerStaffMemberDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
