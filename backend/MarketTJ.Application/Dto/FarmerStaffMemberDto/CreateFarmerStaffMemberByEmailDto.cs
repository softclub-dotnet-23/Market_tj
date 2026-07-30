using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.FarmerStaffMemberDto;

public class CreateFarmerStaffMemberByEmailDto
{
    public int FarmerProfileId { get; set; }
    public string Email { get; set; } = null!;
    public StaffPermissions Permissions { get; set; }
    public bool IsActive { get; set; }
}
