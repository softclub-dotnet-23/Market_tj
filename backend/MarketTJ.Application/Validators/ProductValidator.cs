using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class ProductValidator
{
    public static Result<string>? ValidateCreate(CreateProductDto dto)
        => Validate(dto.CategoryId, dto.Name, dto.Unit);

    public static Result<string>? ValidateUpdate(UpdateProductDto dto)
        => Validate(dto.CategoryId, dto.Name, dto.Unit);

    private static Result<string>? Validate(int categoryId, string name, string unit)
    {
        if (categoryId <= 0)
            return Result<string>.Fail("CategoryId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Fail("Name обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(unit))
            return Result<string>.Fail("Unit обязателен", ErrorType.Validation);

        return null;
    }
}
