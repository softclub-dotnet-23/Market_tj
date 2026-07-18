using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.OrderItemDto;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

public static class OrderItemValidator
{
    public static Result<string>? ValidateCreate(CreateOrderItemDto dto)
        => Validate(dto.OrderId, dto.ProductListingId, dto.ProductName, dto.UnitPrice, dto.Quantity);

    public static Result<string>? ValidateUpdate(UpdateOrderItemDto dto)
        => Validate(dto.OrderId, dto.ProductListingId, dto.ProductName, dto.UnitPrice, dto.Quantity);

    private static Result<string>? Validate(int orderId, int productListingId, string productName, decimal unitPrice, decimal quantity)
    {
        if (orderId <= 0)
            return Result<string>.Fail("OrderId обязателен", ErrorType.Validation);

        if (productListingId <= 0)
            return Result<string>.Fail("ProductListingId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(productName))
            return Result<string>.Fail("ProductName обязателен", ErrorType.Validation);

        if (unitPrice <= 0)
            return Result<string>.Fail("UnitPrice должна быть больше 0", ErrorType.Validation);

        if (quantity <= 0)
            return Result<string>.Fail("Quantity должно быть больше 0", ErrorType.Validation);

        return null;
    }
}
