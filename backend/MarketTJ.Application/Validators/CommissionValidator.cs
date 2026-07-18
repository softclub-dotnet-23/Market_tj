using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CommissionDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class CommissionValidator
{
    public static Result<string>? ValidateCreate(CreateCommissionDto dto)
        => Validate(dto.Percentage, dto.EffectiveFrom, dto.EffectiveTo);

    public static Result<string>? ValidateUpdate(UpdateCommissionDto dto)
        => Validate(dto.Percentage, dto.EffectiveFrom, dto.EffectiveTo);

    private static Result<string>? Validate(decimal percentage, DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (percentage < 0 || percentage > 100)
            return Result<string>.Fail("Percentage должен быть от 0 до 100", ErrorType.Validation);

        if (effectiveFrom == default)
            return Result<string>.Fail("EffectiveFrom обязателен", ErrorType.Validation);

        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
            return Result<string>.Fail("EffectiveTo должен быть позже EffectiveFrom", ErrorType.Validation);

        return null;
    }
}
