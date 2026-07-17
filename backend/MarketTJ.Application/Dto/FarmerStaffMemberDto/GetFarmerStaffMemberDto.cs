using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Dto.FarmerStaffMemberDto;

public class GetFarmerStaffMemberDto
{
    public int Id { get; set; }
    public int FarmerProfileId { get; set; }
    public int UserId { get; set; }
    public StaffPermissions Permissions { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
