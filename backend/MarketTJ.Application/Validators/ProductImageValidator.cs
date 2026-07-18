using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductImageDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class ProductImageValidator
{
    // Раздел 8.8 ТЗ: разрешённые форматы.
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public static Result<string>? ValidateCreate(CreateProductImageDto dto)
        => Validate(dto.ProductListingId, dto.ImageUrl);

    public static Result<string>? ValidateUpdate(UpdateProductImageDto dto)
        => Validate(dto.ProductListingId, dto.ImageUrl);

    private static Result<string>? Validate(int productListingId, string imageUrl)
    {
        if (productListingId <= 0)
            return Result<string>.Fail("ProductListingId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(imageUrl))
            return Result<string>.Fail("ImageUrl обязателен", ErrorType.Validation);

        if (!AllowedExtensions.Any(ext => imageUrl.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            return Result<string>.Fail("Разрешены только форматы: jpg, jpeg, png, webp", ErrorType.Validation);

        return null;
    }
}
