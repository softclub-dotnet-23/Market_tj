using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FavoriteDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class FavoriteValidator
{
    public static Result<string>? ValidateCreate(CreateFavoriteDto dto)
        => Validate(dto.CustomerId, dto.ProductListingId);

    public static Result<string>? ValidateUpdate(UpdateFavoriteDto dto)
        => Validate(dto.CustomerId, dto.ProductListingId);

    private static Result<string>? Validate(int customerId, int productListingId)
    {
        if (customerId <= 0)
            return Result<string>.Fail("CustomerId обязателен", ErrorType.Validation);

        if (productListingId <= 0)
            return Result<string>.Fail("ProductListingId обязателен", ErrorType.Validation);

        return null;
    }
}
