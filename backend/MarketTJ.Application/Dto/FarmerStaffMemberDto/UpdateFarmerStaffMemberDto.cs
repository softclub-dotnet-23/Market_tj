using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.FarmerStaffMemberDto;

public class UpdateFarmerStaffMemberDto
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    public int UserId { get; set; }
    public StaffPermissions Permissions { get; set; }
    public bool IsActive { get; set; }
}
