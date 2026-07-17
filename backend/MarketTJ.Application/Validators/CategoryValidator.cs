using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CategoryDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class CategoryValidator
{
    public static Result<string>? ValidateCreate(CreateCategoryDto dto)
        => Validate(dto.Name);

    public static Result<string>? ValidateUpdate(UpdateCategoryDto dto)
        => Validate(dto.Name);

    private static Result<string>? Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Fail("Name обязателен", ErrorType.Validation);

        return null;
    }
}
