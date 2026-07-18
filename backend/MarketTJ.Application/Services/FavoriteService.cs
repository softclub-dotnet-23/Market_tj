using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FavoriteDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class FavoriteService(
    IFavoriteRepository favoriteRepository,
    IUserRepository userRepository,
    IProductListingRepository productListingRepository,
    ILogger<FavoriteService> logger) : IFavoriteService
{
    public async Task<Result<IEnumerable<GetFavoriteDto>>> GetAllAsync()
    {
        try
        {
            var favorites = await favoriteRepository.GetAllAsync();
            return Result<IEnumerable<GetFavoriteDto>>.Ok(favorites.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка избранного");
            return Result<IEnumerable<GetFavoriteDto>>.Fail("Не удалось получить список избранного", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetFavoriteDto?>> GetByIdAsync(int id)
    {
        try
        {
            var favorite = await favoriteRepository.GetByIdAsync(id);
            if (favorite is null)
                return Result<GetFavoriteDto?>.Fail("Запись избранного не найдена", ErrorType.NotFound);

            return Result<GetFavoriteDto?>.Ok(ToGetDto(favorite));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении записи избранного {Id}", id);
            return Result<GetFavoriteDto?>.Fail("Не удалось получить запись избранного", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateFavoriteDto dto)
    {
        try
        {
            var validation = FavoriteValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var customer = await userRepository.GetByIdAsync(dto.CustomerId);
            if (customer is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            // Раздел 8.25 ТЗ: уникальность пары CustomerId+ProductListingId.
            var all = await favoriteRepository.GetAllAsync();
            if (all.Any(f => f.CustomerId == dto.CustomerId && f.ProductListingId == dto.ProductListingId))
                return Result<string>.Fail("Объявление уже добавлено в избранное", ErrorType.Conflict);

            var favorite = new Favorite
            {
                CustomerId = dto.CustomerId,
                ProductListingId = dto.ProductListingId,
                CreatedAt = DateTime.UtcNow
            };

            await favoriteRepository.AddAsync(favorite);
            return Result<string>.Ok("Добавлено в избранное");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании записи избранного");
            return Result<string>.Fail("Не удалось добавить в избранное", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateFavoriteDto dto)
    {
        try
        {
            var validation = FavoriteValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var favorite = await favoriteRepository.GetByIdAsync(id);
            if (favorite is null)
                return Result<string>.Fail("Запись избранного не найдена", ErrorType.NotFound);

            var customer = await userRepository.GetByIdAsync(dto.CustomerId);
            if (customer is null)
                return Result<string>.Fail("Пользователь не найден", ErrorType.NotFound);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            var all = await favoriteRepository.GetAllAsync();
            if (all.Any(f => f.Id != id && f.CustomerId == dto.CustomerId && f.ProductListingId == dto.ProductListingId))
                return Result<string>.Fail("Объявление уже добавлено в избранное", ErrorType.Conflict);

            favorite.CustomerId = dto.CustomerId;
            favorite.ProductListingId = dto.ProductListingId;

            await favoriteRepository.UpdateAsync(favorite);
            return Result<string>.Ok("Запись избранного обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении записи избранного {Id}", id);
            return Result<string>.Fail("Не удалось обновить запись избранного", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var favorite = await favoriteRepository.GetByIdAsync(id);
            if (favorite is null)
                return Result<string>.Fail("Запись избранного не найдена", ErrorType.NotFound);

            await favoriteRepository.DeleteAsync(favorite);
            return Result<string>.Ok("Удалено из избранного");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении записи избранного {Id}", id);
            return Result<string>.Fail("Не удалось удалить из избранного", ErrorType.InternalServerError);
        }
    }

    private static GetFavoriteDto ToGetDto(Favorite favorite) => new()
    {
        Id = favorite.Id,
        CustomerId = favorite.CustomerId,
        ProductListingId = favorite.ProductListingId,
        CreatedAt = favorite.CreatedAt
    };
}
