using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerDocumentDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class FarmerDocumentValidator
{
    public static Result<string>? ValidateCreate(CreateFarmerDocumentDto dto)
        => Validate(dto.FarmerProfileId, dto.DocumentType, dto.FileUrl, dto.Status, dto.RejectionReason);

    public static Result<string>? ValidateUpdate(UpdateFarmerDocumentDto dto)
        => Validate(dto.FarmerProfileId, dto.DocumentType, dto.FileUrl, dto.Status, dto.RejectionReason);

    private static Result<string>? Validate(int farmerProfileId, FarmerDocumentType documentType, string fileUrl, DocumentReviewStatus status, string? rejectionReason)
    {
        if (farmerProfileId <= 0)
            return Result<string>.Fail("FarmerProfileId обязателен", ErrorType.Validation);

        if (!Enum.IsDefined(documentType))
            return Result<string>.Fail("Указан несуществующий тип документа", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(fileUrl))
            return Result<string>.Fail("FileUrl обязателен", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус проверки", ErrorType.Validation);

        // Раздел 8.18 ТЗ: при отклонении документа причина обязательна.
        if (status == DocumentReviewStatus.Rejected && string.IsNullOrWhiteSpace(rejectionReason))
            return Result<string>.Fail("При отклонении документа необходимо указать RejectionReason", ErrorType.Validation);

        return null;
    }
}
