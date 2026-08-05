using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductListingDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class ProductListingValidator
{
    // Create: AvailableQuantity строго больше 0 (раздел 21 ТЗ — на момент
    // публикации объявление должно быть в наличии).
    public static Result<string>? ValidateCreate(CreateProductListingDto dto)
        => Validate(dto.FarmerProfileId, dto.CategoryId, dto.Unit, dto.Title, dto.TitleTj, dto.TitleEn,
            dto.Description, dto.DescriptionTj, dto.DescriptionEn, dto.RetailPricePerKg,
            dto.WholesalePricePerKg, dto.WholesaleMinimumQuantity, dto.AvailableQuantity, dto.MinimumOrderQuantity,
            dto.QualityGrade, dto.Region, dto.District, dto.Address, dto.Status, requireQuantityPositive: true);

    // Update: AvailableQuantity может опуститься до 0 (раздел 8.7/10.2 —
    // тогда статус переходит в OutOfStock), поэтому здесь >= 0.
    public static Result<string>? ValidateUpdate(UpdateProductListingDto dto)
        => Validate(dto.FarmerProfileId, dto.CategoryId, dto.Unit, dto.Title, dto.TitleTj, dto.TitleEn,
            dto.Description, dto.DescriptionTj, dto.DescriptionEn, dto.RetailPricePerKg,
            dto.WholesalePricePerKg, dto.WholesaleMinimumQuantity, dto.AvailableQuantity, dto.MinimumOrderQuantity,
            dto.QualityGrade, dto.Region, dto.District, dto.Address, dto.Status, requireQuantityPositive: false);

    private static Result<string>? Validate(int farmerProfileId, int categoryId, string unit,
        string? title, string? titleTj, string? titleEn, string? description, string? descriptionTj, string? descriptionEn,
        decimal retailPrice, decimal? wholesalePrice, decimal? wholesaleMinQuantity, decimal availableQuantity,
        decimal minimumOrderQuantity, string qualityGrade, string region, string district, string address,
        ListingStatus status, bool requireQuantityPositive)
    {
        if (farmerProfileId <= 0)
            return Result<string>.Fail("FarmerProfileId обязателен", ErrorType.Validation);

        if (categoryId <= 0)
            return Result<string>.Fail("CategoryId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(unit))
            return Result<string>.Fail("Unit обязателен", ErrorType.Validation);

        // По прямому запросу пользователя (2026-08-05): достаточно заполнить
        // МИНИМУМ один язык названия — остальные (включая русский Title)
        // переводятся автоматически через Groq (см. ProductListingService).
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(titleTj) && string.IsNullOrWhiteSpace(titleEn))
            return Result<string>.Fail("Заполните название товара хотя бы на одном языке", ErrorType.Validation);

        foreach (var (fieldName, value) in new[] { ("Title", title), ("TitleTj", titleTj), ("TitleEn", titleEn) })
            if (value is { Length: < 3 or > 150 })
                return Result<string>.Fail($"{fieldName} должен быть от 3 до 150 символов", ErrorType.Validation);

        foreach (var (fieldName, value) in new[] { ("Description", description), ("DescriptionTj", descriptionTj), ("DescriptionEn", descriptionEn) })
            if (value is { Length: > 2000 })
                return Result<string>.Fail($"{fieldName} не должен превышать 2000 символов", ErrorType.Validation);

        // Раздел 21/8.7 ТЗ.
        if (retailPrice <= 0)
            return Result<string>.Fail("RetailPricePerKg должна быть больше 0", ErrorType.Validation);

        if (wholesalePrice is not null && wholesalePrice <= 0)
            return Result<string>.Fail("WholesalePricePerKg должна быть больше 0 или не указана", ErrorType.Validation);

        // Раздел 8.7 ТЗ: оптовая цена не должна быть выше розничной.
        if (wholesalePrice is not null && wholesalePrice > retailPrice)
            return Result<string>.Fail("WholesalePricePerKg не может быть выше RetailPricePerKg", ErrorType.Validation);

        if (wholesaleMinQuantity is not null && wholesaleMinQuantity <= 0)
            return Result<string>.Fail("WholesaleMinimumQuantity должна быть больше 0, если указана", ErrorType.Validation);

        // Если задана оптовая цена — должен быть задан и минимальный опт. объём, и наоборот.
        if (wholesalePrice is not null && wholesaleMinQuantity is null)
            return Result<string>.Fail("Для WholesalePricePerKg нужно указать WholesaleMinimumQuantity", ErrorType.Validation);

        if (wholesaleMinQuantity is not null && wholesalePrice is null)
            return Result<string>.Fail("WholesaleMinimumQuantity указан, но WholesalePricePerKg отсутствует", ErrorType.Validation);

        if (requireQuantityPositive && availableQuantity <= 0)
            return Result<string>.Fail("AvailableQuantity должна быть больше 0", ErrorType.Validation);

        if (!requireQuantityPositive && availableQuantity < 0)
            return Result<string>.Fail("AvailableQuantity не может быть отрицательной", ErrorType.Validation);

        if (minimumOrderQuantity <= 0)
            return Result<string>.Fail("MinimumOrderQuantity должна быть больше 0", ErrorType.Validation);

        if (minimumOrderQuantity > availableQuantity && availableQuantity > 0)
            return Result<string>.Fail("MinimumOrderQuantity не может превышать AvailableQuantity", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(qualityGrade))
            return Result<string>.Fail("QualityGrade обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(region))
            return Result<string>.Fail("Region обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(district))
            return Result<string>.Fail("District обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(address))
            return Result<string>.Fail("Address обязателен", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус объявления", ErrorType.Validation);

        return null;
    }
}
