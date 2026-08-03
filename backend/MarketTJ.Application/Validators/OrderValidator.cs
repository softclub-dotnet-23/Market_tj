using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.OrderDto;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Validators;

public static class OrderValidator
{
    public static Result<string>? ValidateCreate(CreateOrderDto dto)
    {
        var baseValidation = Validate(dto.OrderNumber, dto.CustomerId, dto.FarmerId, dto.DeliveryAddress, dto.Region, dto.District,
            dto.Subtotal, dto.DeliveryPrice, dto.TotalAmount, dto.Status);
        if (baseValidation is not null)
            return baseValidation;

        if (!Enum.IsDefined(dto.PaymentMethod))
            return Result<string>.Fail("Указан несуществующий способ оплаты", ErrorType.Validation);

        // Card требует конкретную карту — без неё непонятно, откуда списывать
        // (см. запрос: "передавать walletId явно при оформлении заказа").
        // CashOnDelivery, наоборот, не должен требовать WalletId вообще —
        // эта опция доступна даже покупателю без единой карты.
        if (dto.PaymentMethod == OrderPaymentMethod.Card && dto.WalletId is null or <= 0)
            return Result<string>.Fail("Для оплаты картой нужно выбрать карту", ErrorType.Validation);

        return null;
    }

    public static Result<string>? ValidateUpdate(UpdateOrderDto dto)
        => Validate(dto.OrderNumber, dto.CustomerId, dto.FarmerId, dto.DeliveryAddress, dto.Region, dto.District,
            dto.Subtotal, dto.DeliveryPrice, dto.TotalAmount, dto.Status);

    // Раздел 21 ТЗ (Order): DeliveryAddress обязателен. Раздел 10.4: суммы не
    // должны быть отрицательными (сервер их всё равно пересчитывает — см.
    // OrderService.CreateAsync).
    private static Result<string>? Validate(string orderNumber, int customerId, int farmerId, string deliveryAddress,
        string region, string district, decimal subtotal, decimal deliveryPrice, decimal totalAmount, OrderStatus status)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return Result<string>.Fail("OrderNumber обязателен", ErrorType.Validation);

        if (customerId <= 0)
            return Result<string>.Fail("CustomerId обязателен", ErrorType.Validation);

        if (farmerId <= 0)
            return Result<string>.Fail("FarmerId обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(deliveryAddress))
            return Result<string>.Fail("DeliveryAddress обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(region))
            return Result<string>.Fail("Region обязателен", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(district))
            return Result<string>.Fail("District обязателен", ErrorType.Validation);

        if (subtotal < 0)
            return Result<string>.Fail("Subtotal не может быть отрицательным", ErrorType.Validation);

        if (deliveryPrice < 0)
            return Result<string>.Fail("DeliveryPrice не может быть отрицательным", ErrorType.Validation);

        if (totalAmount < 0)
            return Result<string>.Fail("TotalAmount не может быть отрицательным", ErrorType.Validation);

        if (!Enum.IsDefined(status))
            return Result<string>.Fail("Указан несуществующий статус заказа", ErrorType.Validation);

        return null;
    }
}
