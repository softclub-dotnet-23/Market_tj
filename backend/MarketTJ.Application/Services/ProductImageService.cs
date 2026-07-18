using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductImageDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class ProductImageService(
    IProductImageRepository productImageRepository,
    IProductListingRepository productListingRepository,
    ILogger<ProductImageService> logger) : IProductImageService
{
    // Раздел 8.8 ТЗ: максимум 5 изображений на объявление.
    private const int MaxImagesPerListing = 5;

    public async Task<Result<IEnumerable<GetProductImageDto>>> GetAllAsync()
    {
        try
        {
            var images = await productImageRepository.GetAllAsync();
            return Result<IEnumerable<GetProductImageDto>>.Ok(images.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка изображений");
            return Result<IEnumerable<GetProductImageDto>>.Fail("Не удалось получить список изображений", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetProductImageDto?>> GetByIdAsync(int id)
    {
        try
        {
            var image = await productImageRepository.GetByIdAsync(id);
            if (image is null)
                return Result<GetProductImageDto?>.Fail("Изображение не найдено", ErrorType.NotFound);

            return Result<GetProductImageDto?>.Ok(ToGetDto(image));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении изображения {Id}", id);
            return Result<GetProductImageDto?>.Fail("Не удалось получить изображение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateProductImageDto dto)
    {
        try
        {
            var validation = ProductImageValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            var all = await productImageRepository.GetAllAsync();
            var currentCount = all.Count(i => i.ProductListingId == dto.ProductListingId);
            if (currentCount >= MaxImagesPerListing)
                return Result<string>.Fail("У объявления уже максимум 5 изображений", ErrorType.Validation);

            var image = new ProductImage
            {
                ProductListingId = dto.ProductListingId,
                ImageUrl = dto.ImageUrl,
                IsMain = dto.IsMain,
                CreatedAt = DateTime.UtcNow
            };

            await productImageRepository.AddAsync(image);
            return Result<string>.Ok("Изображение добавлено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при добавлении изображения");
            return Result<string>.Fail("Не удалось добавить изображение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateProductImageDto dto)
    {
        try
        {
            var validation = ProductImageValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var image = await productImageRepository.GetByIdAsync(id);
            if (image is null)
                return Result<string>.Fail("Изображение не найдено", ErrorType.NotFound);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            image.ProductListingId = dto.ProductListingId;
            image.ImageUrl = dto.ImageUrl;
            image.IsMain = dto.IsMain;

            await productImageRepository.UpdateAsync(image);
            return Result<string>.Ok("Изображение обновлено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении изображения {Id}", id);
            return Result<string>.Fail("Не удалось обновить изображение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var image = await productImageRepository.GetByIdAsync(id);
            if (image is null)
                return Result<string>.Fail("Изображение не найдено", ErrorType.NotFound);

            await productImageRepository.DeleteAsync(image);
            return Result<string>.Ok("Изображение удалено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении изображения {Id}", id);
            return Result<string>.Fail("Не удалось удалить изображение", ErrorType.InternalServerError);
        }
    }

    private static GetProductImageDto ToGetDto(ProductImage image) => new()
    {
        Id = image.Id,
        ProductListingId = image.ProductListingId,
        ImageUrl = image.ImageUrl,
        IsMain = image.IsMain,
        CreatedAt = image.CreatedAt
    };
}
