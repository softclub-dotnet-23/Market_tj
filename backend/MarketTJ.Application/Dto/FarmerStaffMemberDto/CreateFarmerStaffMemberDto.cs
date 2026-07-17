using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.FarmerStaffMemberDto;

public class CreateFarmerStaffMemberDto
{
    public int FarmerProfileId { get; set; }
    public int UserId { get; set; }
    public StaffPermissions Permissions { get; set; }
    public bool IsActive { get; set; }
}
