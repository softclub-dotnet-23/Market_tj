using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ReportedListingDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class ReportedListingValidator
{
    public static Result<string>? ValidateCreate(CreateReportedListingDto dto)
        => Validate(dto.ProductListingId, dto.ReportedByUserId, dto.Reason, dto.Status);

    public static Result<string>? ValidateUpdate(UpdateReportedListingDto dto)
        => Validate(dto.ProductListingId, dto.ReportedByUserId, dto.Reason, dto.Status);

    private static Result<string>? Validate(int productListingId, int reportedByUserId, ReportReason reason, ReportStatus status)
    {
        if (productListingId <= 0)
            return Result<string>.Fail("ProductListingId обязателен", ErrorType.Validation);

        if (reportedByUserId <= 0)
            return Result<string>.Fail("ReportedByUserId обязателен", ErrorType.Validation);

        if (!Enum.IsDefined(reason))
            return Result<string>.Fail("Указана несуществующая причина жалобы", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус жалобы", ErrorType.Validation);

        return null;
    }
}
