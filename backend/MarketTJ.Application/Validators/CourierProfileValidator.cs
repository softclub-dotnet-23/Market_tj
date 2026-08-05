using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CourierProfileDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class CourierProfileValidator
{
    // По прямому запросу пользователя (2026-08-05): курьер может доставлять
    // только на одном из этих 3 типов транспорта — пешком/мотоцикл/велосипед
    // больше не допускаются. Существующие профили с другим значением не
    // трогаем (см. отчёт), но новый Create/Update с ними отклоняется.
    private static readonly string[] AllowedTransportTypes = ["Автомобиль", "Портер", "КамАЗ"];

    public static Result<string>? ValidateCreate(CreateCourierProfileDto dto)
        => Validate(dto.UserId, dto.TransportType, dto.VehicleNumber, dto.Region, dto.District);

    public static Result<string>? ValidateUpdate(UpdateCourierProfileDto dto)
        => Validate(dto.UserId, dto.TransportType, dto.VehicleNumber, dto.Region, dto.District);

    private static Result<string>? Validate(int userId, string transportType, string vehicleNumber, string region, string district)
    {
        if (userId <= 0)
            return Result<string>.Fail("UserId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(transportType))
            return Result<string>.Fail("TransportType обязателен", ErrorType.Validation);

        if (!AllowedTransportTypes.Contains(transportType))
            return Result<string>.Fail(
                $"TransportType должен быть одним из: {string.Join(", ", AllowedTransportTypes)}",
                ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(vehicleNumber))
            return Result<string>.Fail("VehicleNumber обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(region))
            return Result<string>.Fail("Region обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(district))
            return Result<string>.Fail("District обязателен", ErrorType.Validation);

        return null;
    }
}
