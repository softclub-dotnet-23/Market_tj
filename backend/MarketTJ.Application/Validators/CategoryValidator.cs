using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CategoryDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class CategoryValidator
{
    public static Result<string>? ValidateCreate(CreateCategoryDto dto)
        => Validate(dto.Name, dto.NameTj, dto.NameEn);

    public static Result<string>? ValidateUpdate(UpdateCategoryDto dto)
        => Validate(dto.Name, dto.NameTj, dto.NameEn);

    private static Result<string>? Validate(string name, string? nameTj, string? nameEn)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Fail("Название на русском обязательно", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(nameTj))
            return Result<string>.Fail("Название на таджикском обязательно", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(nameEn))
            return Result<string>.Fail("Название на английском обязательно", ErrorType.Validation);

        return null;
    }
}
