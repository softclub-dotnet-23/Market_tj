using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerProfileDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class FarmerProfileValidator
{
    public static Result<string>? ValidateCreate(CreateFarmerProfileDto dto)
        => Validate(dto.UserId, dto.FarmName, dto.Region, dto.District, dto.Village, dto.Address, dto.VerificationStatus, dto.VerifiedAt, dto.VerifiedByAdminId);

    public static Result<string>? ValidateUpdate(UpdateFarmerProfileDto dto)
        => Validate(dto.UserId, dto.FarmName, dto.Region, dto.District, dto.Village, dto.Address, dto.VerificationStatus, dto.VerifiedAt, dto.VerifiedByAdminId);

    // Раздел 8.2 ТЗ: FarmName/Region/District/Village/Address — обязательные
    // поля (в Entity — не nullable). Раздел 10.1: подтверждает только Admin —
    // если статус не Pending, должен быть указан VerifiedByAdminId.
    private static Result<string>? Validate(int userId, string farmName, string region, string district, string village,
        string address, FarmerVerificationStatus status, DateTime? verifiedAt, int? verifiedByAdminId)
    {
        if (userId <= 0)
            return Result<string>.Fail("UserId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(farmName))
            return Result<string>.Fail("FarmName обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(region))
            return Result<string>.Fail("Region обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(district))
            return Result<string>.Fail("District обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(village))
            return Result<string>.Fail("Village обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(address))
            return Result<string>.Fail("Address обязателен", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус верификации", ErrorType.Validation);

        if (status != FarmerVerificationStatus.Pending && verifiedByAdminId is null)
            return Result<string>.Fail("Для подтверждённого/отклонённого статуса нужен VerifiedByAdminId", ErrorType.Validation);

        if (status != FarmerVerificationStatus.Pending && verifiedAt is null)
            return Result<string>.Fail("Для подтверждённого/отклонённого статуса нужен VerifiedAt", ErrorType.Validation);

        return null;
    }
}
