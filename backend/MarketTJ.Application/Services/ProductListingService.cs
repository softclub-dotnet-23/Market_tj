using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductListingDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class ProductListingService(
    IProductListingRepository productListingRepository,
    IFarmerProfileRepository farmerProfileRepository,
    IProductRepository productRepository,
    ILogger<ProductListingService> logger) : IProductListingService
{
    public async Task<Result<PagedResult<GetProductListingDto>>> GetAllAsync(int pageNumber, int pageSize)
    {
        try
        {
            if (pageNumber <= 0)
                return Result<PagedResult<GetProductListingDto>>.Fail("pageNumber должен быть больше 0", ErrorType.Validation);

            if (pageSize <= 0)
                return Result<PagedResult<GetProductListingDto>>.Fail("pageSize должен быть больше 0", ErrorType.Validation);

            // IProductListingRepository.GetAllAsync() без параметров пагинации —
            // Skip/Take применяются в памяти, репозиторий не расширяю сверх того,
            // что там реально есть (раздел 13.5 ТЗ добавит серверную пагинацию
            // позже, на Этапе 4 раздела 23).
            var all = await productListingRepository.GetAllAsync();
            var page = all
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(ToGetDto)
                .ToList();

            return Result<PagedResult<GetProductListingDto>>.Ok(
                PagedResult<GetProductListingDto>.Ok(page, all.Count, pageNumber, pageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка объявлений");
            return Result<PagedResult<GetProductListingDto>>.Fail("Не удалось получить список объявлений", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetProductListingDto?>> GetByIdAsync(int id)
    {
        try
        {
            var listing = await productListingRepository.GetByIdAsync(id);
            if (listing is null)
                return Result<GetProductListingDto?>.Fail("Объявление не найдено", ErrorType.NotFound);

            return Result<GetProductListingDto?>.Ok(ToGetDto(listing));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении объявления {Id}", id);
            return Result<GetProductListingDto?>.Fail("Не удалось получить объявление", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateProductListingDto dto)
    {
        try
        {
            var validation = ProductListingValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerProfileId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            var product = await productRepository.GetByIdAsync(dto.ProductId);
            if (product is null)
                return Result<string>.Fail("Продукт не найден", ErrorType.NotFound);

            // Раздел 10.1 ТЗ: неподтверждённый фермер не может создать активное объявление.
            if (dto.Status == ListingStatus.Active && farmerProfile.VerificationStatus != FarmerVerificationStatus.Verified)
                return Result<string>.Fail("Неподтверждённый фермер не может создать активное объявление", ErrorType.Validation);

            var listing = new ProductListing
            {
                FarmerProfileId = dto.FarmerProfileId,
                ProductId = dto.ProductId,
                Title = dto.Title,
                Description = dto.Description,
                RetailPricePerKg = dto.RetailPricePerKg,
                WholesalePricePerKg = dto.WholesalePricePerKg,
                WholesaleMinimumQuantity = dto.WholesaleMinimumQuantity,
                AvailableQuantity = dto.AvailableQuantity,
                MinimumOrderQuantity = dto.MinimumOrderQuantity,
                HarvestDate = dto.HarvestDate,
                ExpectedHarvestDate = dto.ExpectedHarvestDate,
                QualityGrade = dto.QualityGrade,
                Region = dto.Region,
                District = dto.District,
                Address = dto.Address,
                Status = dto.Status,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await productListingRepository.AddAsync(listing);
            return Result<string>.Ok("Объявление создано");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании объявления");
            return Result<string>.Fail("Не удалось создать объявление", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateProductListingDto dto)
    {
        try
        {
            var validation = ProductListingValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var listing = await productListingRepository.GetByIdAsync(id);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerProfileId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            var product = await productRepository.GetByIdAsync(dto.ProductId);
            if (product is null)
                return Result<string>.Fail("Продукт не найден", ErrorType.NotFound);

            if (dto.Status == ListingStatus.Active && farmerProfile.VerificationStatus != FarmerVerificationStatus.Verified)
                return Result<string>.Fail("Неподтверждённый фермер не может создать активное объявление", ErrorType.Validation);

            listing.FarmerProfileId = dto.FarmerProfileId;
            listing.ProductId = dto.ProductId;
            listing.Title = dto.Title;
            listing.Description = dto.Description;
            listing.RetailPricePerKg = dto.RetailPricePerKg;
            listing.WholesalePricePerKg = dto.WholesalePricePerKg;
            listing.WholesaleMinimumQuantity = dto.WholesaleMinimumQuantity;
            // Раздел 10.2 ТЗ: количество 0 переводит объявление в OutOfStock.
            listing.AvailableQuantity = dto.AvailableQuantity;
            listing.Status = dto.AvailableQuantity == 0 && dto.Status == ListingStatus.Active
                ? ListingStatus.OutOfStock
                : dto.Status;
            listing.MinimumOrderQuantity = dto.MinimumOrderQuantity;
            listing.HarvestDate = dto.HarvestDate;
            listing.ExpectedHarvestDate = dto.ExpectedHarvestDate;
            listing.QualityGrade = dto.QualityGrade;
            listing.Region = dto.Region;
            listing.District = dto.District;
            listing.Address = dto.Address;
            listing.UpdatedAt = DateTime.UtcNow;

            await productListingRepository.UpdateAsync(listing);
            return Result<string>.Ok("Объявление обновлено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении объявления {Id}", id);
            return Result<string>.Fail("Не удалось обновить объявление", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var listing = await productListingRepository.GetByIdAsync(id);
            if (listing is null)
                return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

            // Раздел 18 ТЗ: soft delete (у ProductListing есть IsDeleted/DeletedAt).
            listing.IsDeleted = true;
            listing.DeletedAt = DateTime.UtcNow;
            listing.UpdatedAt = DateTime.UtcNow;

            await productListingRepository.UpdateAsync(listing);
            return Result<string>.Ok("Объявление удалено");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении объявления {Id}", id);
            return Result<string>.Fail("Не удалось удалить объявление", ErrorType.InternalServerError);
        }
    }

    private static GetProductListingDto ToGetDto(ProductListing listing) => new()
    {
        Id = listing.Id,
        FarmerProfileId = listing.FarmerProfileId,
        ProductId = listing.ProductId,
        Title = listing.Title,
        Description = listing.Description,
        RetailPricePerKg = listing.RetailPricePerKg,
        WholesalePricePerKg = listing.WholesalePricePerKg,
        WholesaleMinimumQuantity = listing.WholesaleMinimumQuantity,
        AvailableQuantity = listing.AvailableQuantity,
        MinimumOrderQuantity = listing.MinimumOrderQuantity,
        HarvestDate = listing.HarvestDate,
        ExpectedHarvestDate = listing.ExpectedHarvestDate,
        QualityGrade = listing.QualityGrade,
        Region = listing.Region,
        District = listing.District,
        Address = listing.Address,
        Status = listing.Status,
        CreatedAt = listing.CreatedAt,
        UpdatedAt = listing.UpdatedAt
    };
}
