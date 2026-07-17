using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.ProductDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<Result<IEnumerable<GetProductDto>>> GetAllAsync()
    {
        try
        {
            var products = await productRepository.GetAllAsync();
            return Result<IEnumerable<GetProductDto>>.Ok(products.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка продуктов");
            return Result<IEnumerable<GetProductDto>>.Fail("Не удалось получить список продуктов", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetProductDto?>> GetByIdAsync(int id)
    {
        try
        {
            var product = await productRepository.GetByIdAsync(id);
            if (product is null)
                return Result<GetProductDto?>.Fail("Продукт не найден", ErrorType.NotFound);

            return Result<GetProductDto?>.Ok(ToGetDto(product));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении продукта {Id}", id);
            return Result<GetProductDto?>.Fail("Не удалось получить продукт", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateProductDto dto)
    {
        try
        {
            var validation = ProductValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            var category = await categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category is null)
                return Result<string>.Fail("Категория не найдена", ErrorType.NotFound);

            var product = new Product
            {
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                Unit = dto.Unit,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await productRepository.AddAsync(product);
            return Result<string>.Ok("Продукт создан");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании продукта");
            return Result<string>.Fail("Не удалось создать продукт", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateProductDto dto)
    {
        try
        {
            var validation = ProductValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var product = await productRepository.GetByIdAsync(id);
            if (product is null)
                return Result<string>.Fail("Продукт не найден", ErrorType.NotFound);

            var category = await categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category is null)
                return Result<string>.Fail("Категория не найдена", ErrorType.NotFound);

            product.CategoryId = dto.CategoryId;
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Unit = dto.Unit;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await productRepository.UpdateAsync(product);
            return Result<string>.Ok("Продукт обновлён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении продукта {Id}", id);
            return Result<string>.Fail("Не удалось обновить продукт", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var product = await productRepository.GetByIdAsync(id);
            if (product is null)
                return Result<string>.Fail("Продукт не найден", ErrorType.NotFound);

            await productRepository.DeleteAsync(product);
            return Result<string>.Ok("Продукт удалён");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении продукта {Id}", id);
            return Result<string>.Fail("Не удалось удалить продукт", ErrorType.InternalServerError);
        }
    }

    private static GetProductDto ToGetDto(Product product) => new()
    {
        Id = product.Id,
        CategoryId = product.CategoryId,
        Name = product.Name,
        Description = product.Description,
        Unit = product.Unit,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
