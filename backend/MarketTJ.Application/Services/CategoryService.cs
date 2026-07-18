using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CategoryDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Validators;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

public class CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<Result<IEnumerable<GetCategoryDto>>> GetAllAsync()
    {
        try
        {
            var categories = await categoryRepository.GetAllAsync();
            return Result<IEnumerable<GetCategoryDto>>.Ok(categories.Select(ToGetDto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка категорий");
            return Result<IEnumerable<GetCategoryDto>>.Fail("Не удалось получить список категорий", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<GetCategoryDto?>> GetByIdAsync(int id)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(id);
            if (category is null)
                return Result<GetCategoryDto?>.Fail("Категория не найдена", ErrorType.NotFound);

            return Result<GetCategoryDto?>.Ok(ToGetDto(category));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении категории {Id}", id);
            return Result<GetCategoryDto?>.Fail("Не удалось получить категорию", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> CreateAsync(CreateCategoryDto dto)
    {
        try
        {
            var validation = CategoryValidator.ValidateCreate(dto);
            if (validation is not null)
                return validation;

            // Уникальность Name — CategoryConfiguration.HasIndex(Name).IsUnique().
            var all = await categoryRepository.GetAllAsync();
            if (all.Any(c => c.Name == dto.Name))
                return Result<string>.Fail("Категория с таким названием уже существует", ErrorType.Conflict);

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await categoryRepository.AddAsync(category);
            return Result<string>.Ok("Категория создана");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании категории");
            return Result<string>.Fail("Не удалось создать категорию", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        try
        {
            var validation = CategoryValidator.ValidateUpdate(dto);
            if (validation is not null)
                return validation;

            var category = await categoryRepository.GetByIdAsync(id);
            if (category is null)
                return Result<string>.Fail("Категория не найдена", ErrorType.NotFound);

            var all = await categoryRepository.GetAllAsync();
            if (all.Any(c => c.Id != id && c.Name == dto.Name))
                return Result<string>.Fail("Категория с таким названием уже существует", ErrorType.Conflict);

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.ImageUrl = dto.ImageUrl;
            category.IsActive = dto.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await categoryRepository.UpdateAsync(category);
            return Result<string>.Ok("Категория обновлена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обновлении категории {Id}", id);
            return Result<string>.Fail("Не удалось обновить категорию", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> DeleteAsync(int id)
    {
        try
        {
            var category = await categoryRepository.GetByIdAsync(id);
            if (category is null)
                return Result<string>.Fail("Категория не найдена", ErrorType.NotFound);

            await categoryRepository.DeleteAsync(category);
            return Result<string>.Ok("Категория удалена");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при удалении категории {Id}", id);
            return Result<string>.Fail("Не удалось удалить категорию", ErrorType.InternalServerError);
        }
    }

    private static GetCategoryDto ToGetDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        ImageUrl = category.ImageUrl,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt
    };
}
