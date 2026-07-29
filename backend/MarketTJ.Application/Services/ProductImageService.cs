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
    IFarmerProfileRepository farmerProfileRepository,
    IFileStorageService fileStorageService,
    ICurrentUserService currentUser,
    ILogger<ProductImageService> logger) : IProductImageService
{
    // Раздел 8.8 ТЗ: максимум 5 изображений на объявление.
    private const int MaxImagesPerListing = 5;

    // Audit 2026-07-28, находка 2.2 (частично): контроллер уже ограничивал
    // роль (Farmer/Admin), но не проверял, что объявление принадлежит ИМЕННО
    // этому фермеру — один Farmer мог управлять фотографиями чужих объявлений.
    private async Task<bool> OwnsListingAsync(int productListingId)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;

        var listing = await productListingRepository.GetByIdAsync(productListingId);
        if (listing is null)
            return false;

        var myFarmerProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return myFarmerProfile is not null && myFarmerProfile.Id == listing.FarmerProfileId;
    }

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

            if (!await OwnsListingAsync(dto.ProductListingId))
                return Result<string>.Fail("Нельзя управлять изображениями чужого объявления", ErrorType.Forbidden);

            var limitError = await ValidateImageLimitAsync(dto.ProductListingId);
            if (limitError is not null)
                return limitError;

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

            if (!await OwnsListingAsync(image.ProductListingId))
                return Result<string>.Fail("Нельзя управлять изображениями чужого объявления", ErrorType.Forbidden);

            var listing = await productListingRepository.GetByIdAsync(dto.ProductListingId);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            if (!await OwnsListingAsync(dto.ProductListingId))
                return Result<string>.Fail("Нельзя перенести изображение на чужое объявление", ErrorType.Forbidden);

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

            if (!await OwnsListingAsync(image.ProductListingId))
                return Result<string>.Fail("Нельзя управлять изображениями чужого объявления", ErrorType.Forbidden);

            await productImageRepository.DeleteAsync(image);

            // Не мешает записи в БД, даже если ImageUrl — внешняя ссылка, а не
            // файл, загруженный через UploadAsync (Delete тихо игнорирует
            // всё, что не лежит в нашем wwwroot/uploads).
            fileStorageService.Delete(image.ImageUrl);

            return Result<string>.Ok("Изображение удалено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении изображения {Id}", id);
            return Result<string>.Fail("Не удалось удалить изображение", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetProductImageDto>> UploadAsync(int productListingId, bool isMain, Stream fileContent, string fileName, long fileSizeBytes)
    {
        try
        {
            var fileValidation = FileUploadValidator.ValidateImage(fileName, fileSizeBytes);
            if (fileValidation is not null)
                return Result<GetProductImageDto>.Fail(fileValidation.Error!, fileValidation.ErrorType!.Value);

            var listing = await productListingRepository.GetByIdAsync(productListingId);
            if (listing is null)
                return Result<GetProductImageDto>.Fail("Объявление не найдено", ErrorType.NotFound);

            if (!await OwnsListingAsync(productListingId))
                return Result<GetProductImageDto>.Fail("Нельзя управлять изображениями чужого объявления", ErrorType.Forbidden);

            var limitError = await ValidateImageLimitAsync(productListingId);
            if (limitError is not null)
                return Result<GetProductImageDto>.Fail(limitError.Error!, limitError.ErrorType!.Value);

            var imageUrl = await fileStorageService.SaveAsync(fileContent, fileName, $"listings/{productListingId}");

            var image = new ProductImage
            {
                ProductListingId = productListingId,
                ImageUrl = imageUrl,
                IsMain = isMain,
                CreatedAt = DateTime.UtcNow
            };

            await productImageRepository.AddAsync(image);
            return Result<GetProductImageDto>.Ok(ToGetDto(image));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке изображения для объявления {ListingId}", productListingId);
            return Result<GetProductImageDto>.Fail("Не удалось загрузить изображение", ErrorType.InternalServerError);
        }
    }

    private async Task<Result<string>?> ValidateImageLimitAsync(int productListingId)
    {
        var all = await productImageRepository.GetAllAsync();
        var currentCount = all.Count(i => i.ProductListingId == productListingId);
        return currentCount >= MaxImagesPerListing
            ? Result<string>.Fail("У объявления уже максимум 5 изображений", ErrorType.Validation)
            : null;
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
