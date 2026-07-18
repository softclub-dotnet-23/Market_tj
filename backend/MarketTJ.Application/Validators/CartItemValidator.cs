using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CartItemDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class CartItemValidator
{
    public static Result<string>? ValidateCreate(CreateCartItemDto dto)
        => Validate(dto.CustomerId, dto.ProductListingId, dto.Quantity);

    public static Result<string>? ValidateUpdate(UpdateCartItemDto dto)
        => Validate(dto.CustomerId, dto.ProductListingId, dto.Quantity);

    // Раздел 8.9 ТЗ: количество должно быть больше 0. Остальные проверки
    // (минимальный заказ, доступный остаток — раздел 10.3) требуют доступа к
    // ProductListing и выполняются в сервисе, не здесь.
    private static Result<string>? Validate(int customerId, int productListingId, decimal quantity)
    {
        if (customerId <= 0)
            return Result<string>.Fail("CustomerId обязателен", ErrorType.Validation);

        if (productListingId <= 0)
            return Result<string>.Fail("ProductListingId обязателен", ErrorType.Validation);

        if (quantity <= 0)
            return Result<string>.Fail("Quantity должно быть больше 0", ErrorType.Validation);

        return null;
    }
}
