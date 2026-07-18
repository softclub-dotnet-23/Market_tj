using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.DeliveryZoneDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class DeliveryZoneValidator
{
    public static Result<string>? ValidateCreate(CreateDeliveryZoneDto dto)
        => Validate(dto.Region, dto.District, dto.BasePrice, dto.PricePerKm);

    public static Result<string>? ValidateUpdate(UpdateDeliveryZoneDto dto)
        => Validate(dto.Region, dto.District, dto.BasePrice, dto.PricePerKm);

    private static Result<string>? Validate(string region, string district, decimal basePrice, decimal? pricePerKm)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Result<string>.Fail("Region обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(district))
            return Result<string>.Fail("District обязателен", ErrorType.Validation);

        if (basePrice < 0)
            return Result<string>.Fail("BasePrice не может быть отрицательной", ErrorType.Validation);

        if (pricePerKm is < 0)
            return Result<string>.Fail("PricePerKm не может быть отрицательной", ErrorType.Validation);

        return null;
    }
}
