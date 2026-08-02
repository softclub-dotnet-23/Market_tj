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
    ICategoryRepository categoryRepository,
    IProductImageRepository productImageRepository,
    IFarmerDocumentRepository farmerDocumentRepository,
    ICurrentUserService currentUser,
    ILogger<ProductListingService> logger) : IProductListingService
{
    // По прямому запросу пользователя (2026-08-02): фермер не может добавить
    // товар, пока не отправил паспорт (обе стороны) и селфи — Rejected не
    // считается "отправленным", нужна новая загрузка. Admin создаёт объявления
    // от лица фермера (например, помогая на онбординге) — на него гейт не
    // распространяется.
    private static readonly FarmerDocumentType[] RequiredDocumentTypes =
        [FarmerDocumentType.PassportFront, FarmerDocumentType.PassportBack, FarmerDocumentType.Selfie];

    private async Task<bool> HasRequiredDocumentsAsync(int farmerProfileId)
    {
        var documents = await farmerDocumentRepository.GetByFarmerProfileIdAsync(farmerProfileId);
        var submittedTypes = documents
            .Where(d => d.Status != DocumentReviewStatus.Rejected)
            .Select(d => d.DocumentType)
            .ToHashSet();

        return RequiredDocumentTypes.All(submittedTypes.Contains);
    }
    // GetAll/GetById сознательно ОСТАЮТСЯ публичными — это каталог. IDOR-guard
    // нужен только на Create/Update/Delete (audit 2026-07-28, находка 2.2):
    // Farmer мог редактировать/удалять чужие объявления, зная только их Id.
    private async Task<bool> OwnsAsync(int farmerProfileId)
    {
        if (currentUser.IsAdmin())
            return true;
        if (currentUser.UserId is null)
            return false;

        var myProfile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        return myProfile is not null && myProfile.Id == farmerProfileId;
    }

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
            var pageListings = all
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var page = await EnrichAsync(pageListings);

            return Result<PagedResult<GetProductListingDto>>.Ok(
                PagedResult<GetProductListingDto>.Ok(page, all.Count, pageNumber, pageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка объявлений");
            return Result<PagedResult<GetProductListingDto>>.Fail("Не удалось получить список объявлений", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<PagedResult<GetProductListingDto>>> SearchCatalogAsync(ProductListingSearchFilter filter)
    {
        try
        {
            if (filter.PageNumber <= 0)
                return Result<PagedResult<GetProductListingDto>>.Fail("pageNumber должен быть больше 0", ErrorType.Validation);

            if (filter.PageSize <= 0)
                return Result<PagedResult<GetProductListingDto>>.Fail("pageSize должен быть больше 0", ErrorType.Validation);

            var (items, totalCount) = await productListingRepository.SearchCatalogAsync(filter);
            var imagesByListingId = await GetImagesByListingIdAsync(items.Select(x => x.Listing.Id).ToList());
            var dtos = items
                .Select(x => ToGetDto(x.Listing, imagesByListingId.GetValueOrDefault(x.Listing.Id, []), x.Rating, x.OrderCount))
                .ToList();

            return Result<PagedResult<GetProductListingDto>>.Ok(
                PagedResult<GetProductListingDto>.Ok(dtos, totalCount, filter.PageNumber, filter.PageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при поиске по каталогу");
            return Result<PagedResult<GetProductListingDto>>.Fail("Не удалось выполнить поиск по каталогу", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<List<string>>> GetDistinctActiveRegionsAsync()
    {
        try
        {
            var regions = await productListingRepository.GetDistinctActiveRegionsAsync();
            return Result<List<string>>.Ok(regions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка регионов каталога");
            return Result<List<string>>.Fail("Не удалось получить список регионов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetProductListingDto?>> GetByIdAsync(int id)
    {
        try
        {
            var listing = await productListingRepository.GetByIdAsync(id);
            if (listing is null)
                return Result<GetProductListingDto?>.Fail("Объявление не найдено", ErrorType.NotFound);

            var enriched = await EnrichAsync([listing]);
            return Result<GetProductListingDto?>.Ok(enriched[0]);
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

            if (!await OwnsAsync(dto.FarmerProfileId))
                return Result<string>.Fail("Нельзя создать объявление для чужой фермы", ErrorType.Forbidden);

            if (!currentUser.IsAdmin() && !await HasRequiredDocumentsAsync(dto.FarmerProfileId))
                return Result<string>.Fail(
                    "Чтобы добавить товар, сначала загрузите паспорт (лицевую и обратную стороны) и селфи в разделе «Документы»",
                    ErrorType.Validation);

            var category = await categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category is null)
                return Result<string>.Fail("Категория не найдена", ErrorType.NotFound);

            // Раздел 10.1 ТЗ: неподтверждённый фермер не может создать активное объявление.
            if (dto.Status == ListingStatus.Active && farmerProfile.VerificationStatus != FarmerVerificationStatus.Verified)
                return Result<string>.Fail("Неподтверждённый фермер не может создать активное объявление", ErrorType.Validation);

            var listing = new ProductListing
            {
                FarmerProfileId = dto.FarmerProfileId,
                CategoryId = dto.CategoryId,
                Unit = dto.Unit,
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

            if (!await OwnsAsync(listing.FarmerProfileId))
                return Result<string>.Fail("Нет доступа к этому объявлению", ErrorType.Forbidden);

            var farmerProfile = await farmerProfileRepository.GetByIdAsync(dto.FarmerProfileId);
            if (farmerProfile is null)
                return Result<string>.Fail("Профиль фермера не найден", ErrorType.NotFound);

            var category = await categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category is null)
                return Result<string>.Fail("Категория не найдена", ErrorType.NotFound);

            if (dto.Status == ListingStatus.Active && farmerProfile.VerificationStatus != FarmerVerificationStatus.Verified)
                return Result<string>.Fail("Неподтверждённый фермер не может создать активное объявление", ErrorType.Validation);

            listing.FarmerProfileId = dto.FarmerProfileId;
            listing.CategoryId = dto.CategoryId;
            listing.Unit = dto.Unit;
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

            if (!await OwnsAsync(listing.FarmerProfileId))
                return Result<string>.Fail("Нет доступа к этому объявлению", ErrorType.Forbidden);

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

    // Общее обогащение для GetAllAsync/GetByIdAsync — ImageUrls/Rating/OrderCount
    // только для переданных объявлений, а не для всего каталога (audit
    // 2026-08-02: раньше это тянул фронт целиком через /product-images и /reviews).
    private async Task<List<GetProductListingDto>> EnrichAsync(List<ProductListing> listings)
    {
        var listingIds = listings.Select(l => l.Id).ToList();
        var farmerIds = listings.Select(l => l.FarmerProfileId).Distinct().ToList();

        var imagesByListingId = await GetImagesByListingIdAsync(listingIds);
        var orderCounts = listingIds.Count > 0
            ? await productListingRepository.GetOrderCountsByListingIdsAsync(listingIds)
            : [];
        var ratings = farmerIds.Count > 0
            ? await productListingRepository.GetRatingsByFarmerIdsAsync(farmerIds)
            : [];

        return listings
            .Select(l => ToGetDto(
                l,
                imagesByListingId.GetValueOrDefault(l.Id, []),
                ratings.GetValueOrDefault(l.FarmerProfileId, 0),
                orderCounts.GetValueOrDefault(l.Id, 0)))
            .ToList();
    }

    private async Task<Dictionary<int, List<string>>> GetImagesByListingIdAsync(List<int> listingIds)
    {
        if (listingIds.Count == 0)
            return [];

        var images = await productImageRepository.GetByListingIdsAsync(listingIds);
        return images
            .GroupBy(i => i.ProductListingId)
            .ToDictionary(g => g.Key, g => g.Select(i => i.ImageUrl).ToList());
    }

    private static GetProductListingDto ToGetDto(ProductListing listing, List<string>? imageUrls = null, double rating = 0, int orderCount = 0) => new()
    {
        Id = listing.Id,
        FarmerProfileId = listing.FarmerProfileId,
        CategoryId = listing.CategoryId,
        Unit = listing.Unit,
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
        UpdatedAt = listing.UpdatedAt,
        ImageUrls = imageUrls ?? [],
        Rating = rating,
        OrderCount = orderCount,
    };
}
