using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CartItemDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class CartItemService(
    ICartItemRepository cartItemRepository,
    ICustomerProfileRepository customerProfileRepository,
    IProductListingRepository productListingRepository,
    ILogger<CartItemService> logger) : ICartItemService
{
    public async Task<Result<IEnumerable<GetCartItemDto>>> GetAllAsync()
    {
        try
        {
            var items = await cartItemRepository.GetAllAsync();
            return Result<IEnumerable<GetCartItemDto>>.Ok(items.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении корзины");
            return Result<IEnumerable<GetCartItemDto>>.Fail("Не удалось получить корзину", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetCartItemDto?>> GetByIdAsync(int id)
    {
        try
        {
            var item = await cartItemRepository.GetByIdAsync(id);
            if (item is null)
                return Result<GetCartItemDto?>.Fail("Позиция корзины не найдена", ErrorType.NotFound);

            return Result<GetCartItemDto?>.Ok(ToGetDto(item));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении позиции корзины {Id}", id);
            return Result<GetCartItemDto?>.Fail("Не удалось получить позицию корзины", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateCartItemDto dto)
    {
        try
        {
            var validation = CartItemValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var customer = await customerProfileRepository.GetByIdAsync(dto.CustomerId);
            if (customer is null)
                return Result<string>.Fail("Профиль покупателя не найден", ErrorType.NotFound);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            // Раздел 10.3 ТЗ.
            if (dto.Quantity < listing.MinimumOrderQuantity)
                return Result<string>.Fail("Quantity не может быть меньше минимального заказа объявления", ErrorType.Validation);

            if (dto.Quantity > listing.AvailableQuantity)
                return Result<string>.Fail("Quantity не может превышать доступный остаток объявления", ErrorType.Validation);

            // Раздел 8.9 ТЗ: один и тот же продукт не должен повторяться в корзине.
            var all = await cartItemRepository.GetAllAsync();
            if (all.Any(c => c.CustomerId == dto.CustomerId && c.ProductListingId == dto.ProductListingId))
                return Result<string>.Fail("Этот товар уже есть в корзине — измените количество вместо повторного добавления", ErrorType.Conflict);

            var item = new CartItem
            {
                CustomerId = dto.CustomerId,
                ProductListingId = dto.ProductListingId,
                Quantity = dto.Quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await cartItemRepository.AddAsync(item);
            return Result<string>.Ok("Товар добавлен в корзину");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении в корзину");
            return Result<string>.Fail("Не удалось добавить товар в корзину", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateCartItemDto dto)
    {
        try
        {
            var validation = CartItemValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var item = await cartItemRepository.GetByIdAsync(id);
            if (item is null)
                return Result<string>.Fail("Позиция корзины не найдена", ErrorType.NotFound);

            var customer = await customerProfileRepository.GetByIdAsync(dto.CustomerId);
            if (customer is null)
                return Result<string>.Fail("Профиль покупателя не найден", ErrorType.NotFound);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            if (dto.Quantity < listing.MinimumOrderQuantity)
                return Result<string>.Fail("Quantity не может быть меньше минимального заказа объявления", ErrorType.Validation);

            if (dto.Quantity > listing.AvailableQuantity)
                return Result<string>.Fail("Quantity не может превышать доступный остаток объявления", ErrorType.Validation);

            var all = await cartItemRepository.GetAllAsync();
            if (all.Any(c => c.Id != id && c.CustomerId == dto.CustomerId && c.ProductListingId == dto.ProductListingId))
                return Result<string>.Fail("Этот товар уже есть в корзине другой позицией", ErrorType.Conflict);

            item.CustomerId = dto.CustomerId;
            item.ProductListingId = dto.ProductListingId;
            item.Quantity = dto.Quantity;
            item.UpdatedAt = DateTime.UtcNow;

            await cartItemRepository.UpdateAsync(item);
            return Result<string>.Ok("Позиция корзины обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении позиции корзины {Id}", id);
            return Result<string>.Fail("Не удалось обновить позицию корзины", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var item = await cartItemRepository.GetByIdAsync(id);
            if (item is null)
                return Result<string>.Fail("Позиция корзины не найдена", ErrorType.NotFound);

            await cartItemRepository.DeleteAsync(item);
            return Result<string>.Ok("Товар удалён из корзины");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении позиции корзины {Id}", id);
            return Result<string>.Fail("Не удалось удалить товар из корзины", ErrorType.InternalServerError);
        }
    }

    private static GetCartItemDto ToGetDto(CartItem item) => new()
    {
        Id = item.Id,
        CustomerId = item.CustomerId,
        ProductListingId = item.ProductListingId,
        Quantity = item.Quantity,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };
}
