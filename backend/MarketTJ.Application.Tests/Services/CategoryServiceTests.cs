using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.CategoryDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ILogger<CategoryService>> _logger = new();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(_categoryRepository.Object, _logger.Object);
        _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static Category CreateCategory(int id = 1, string name = "Овощи") => new()
    {
        Id = id,
        Name = name,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateCategoryDto ValidCreateDto(string name = "Овощи") => new()
    {
        Name = name,
        IsActive = true
    };

    private static UpdateCategoryDto ValidUpdateDto(int id = 1, string name = "Овощи") => new()
    {
        Id = id,
        Name = name,
        IsActive = true
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_CategoriesExist_ReturnsMappedDtos()
    {
        _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateCategory(1), CreateCategory(2, "Фрукты")]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _categoryRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var category = CreateCategory(5, "Молочные продукты");
        _categoryRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(category);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(category.Id, result.Data!.Id);
        Assert.Equal(category.Name, result.Data!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsCategoryAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _categoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmptyName_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Name = "";

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _categoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsConflict()
    {
        _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateCategory(1, "Овощи")]);

        var result = await _service.CreateAsync(ValidCreateDto("Овощи"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _categoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _categoryRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesCategoryAndReturnsOk()
    {
        var category = CreateCategory(1, "Овощи");
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, "Овощи"));

        Assert.True(result.IsSuccess);
        _categoryRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CategoryNotFound_ReturnsNotFound()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _categoryRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmptyName_ReturnsValidationError()
    {
        var dto = ValidUpdateDto(1);
        dto.Name = "";

        var result = await _service.UpdateAsync(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _categoryRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateNameOnAnotherCategory_ReturnsConflict()
    {
        var category = CreateCategory(1, "Овощи");
        var other = CreateCategory(2, "Фрукты");
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        _categoryRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([category, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, "Фрукты"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _categoryRepository.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingCategory_DeletesAndReturnsOk()
    {
        var category = CreateCategory(1);
        _categoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _categoryRepository.Verify(r => r.DeleteAsync(category), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CategoryNotFound_ReturnsNotFound()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _categoryRepository.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _categoryRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
