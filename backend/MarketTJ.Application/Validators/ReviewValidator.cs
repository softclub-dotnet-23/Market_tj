using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ReviewDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class ReviewValidator
{
    public static Result<string>? ValidateCreate(CreateReviewDto dto)
        => Validate(dto.OrderId, dto.CustomerId, dto.FarmerId, dto.Rating);

    public static Result<string>? ValidateUpdate(UpdateReviewDto dto)
        => Validate(dto.OrderId, dto.CustomerId, dto.FarmerId, dto.Rating);

    private static Result<string>? Validate(int orderId, int customerId, int farmerId, int rating)
    {
        if (orderId <= 0)
            return Result<string>.Fail("OrderId обязателен", ErrorType.Validation);

        if (customerId <= 0)
            return Result<string>.Fail("CustomerId обязателен", ErrorType.Validation);

        if (farmerId <= 0)
            return Result<string>.Fail("FarmerId обязателен", ErrorType.Validation);

        // Раздел 10.6 / 8.13 ТЗ: рейтинг от 1 до 5.
        if (rating is < 1 or > 5)
            return Result<string>.Fail("Rating должен быть от 1 до 5", ErrorType.Validation);

        return null;
    }
}
