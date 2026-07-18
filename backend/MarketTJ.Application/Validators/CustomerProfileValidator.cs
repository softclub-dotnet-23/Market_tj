using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CustomerProfileDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class CustomerProfileValidator
{
    public static Result<string>? ValidateCreate(CreateCustomerProfileDto dto)
        => Validate(dto.UserId, dto.Region, dto.District, dto.CustomerType);

    public static Result<string>? ValidateUpdate(UpdateCustomerProfileDto dto)
        => Validate(dto.UserId, dto.Region, dto.District, dto.CustomerType);

    private static Result<string>? Validate(int userId, string region, string district, CustomerType customerType)
    {
        if (userId <= 0)
            return Result<string>.Fail("UserId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(region))
            return Result<string>.Fail("Region обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(district))
            return Result<string>.Fail("District обязателен", ErrorType.Validation);

        if (!Enum.IsDefined(customerType))
            return Result<string>.Fail("Указан несуществующий тип клиента", ErrorType.Validation);

        return null;
    }
}
