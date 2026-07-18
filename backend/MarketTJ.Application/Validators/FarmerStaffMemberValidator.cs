using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerStaffMemberDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class FarmerStaffMemberValidator
{
    private const StaffPermissions AllPermissions = StaffPermissions.ManageProducts | StaffPermissions.ManageStock;

    public static Result<string>? ValidateCreate(CreateFarmerStaffMemberDto dto)
        => Validate(dto.FarmerProfileId, dto.UserId, dto.Permissions);

    public static Result<string>? ValidateUpdate(UpdateFarmerStaffMemberDto dto)
        => Validate(dto.FarmerProfileId, dto.UserId, dto.Permissions);

    private static Result<string>? Validate(int farmerProfileId, int userId, StaffPermissions permissions)
    {
        if (farmerProfileId <= 0)
            return Result<string>.Fail("FarmerProfileId обязателен", ErrorType.Validation);

        if (userId <= 0)
            return Result<string>.Fail("UserId обязателен", ErrorType.Validation);

        // StaffPermissions — [Flags]-enum, проверяем, что не выставлены биты
        // за пределами известных значений.
        if ((permissions & ~AllPermissions) != 0)
            return Result<string>.Fail("Указаны несуществующие права доступа", ErrorType.Validation);

        return null;
    }
}
