using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.FarmerStaffMemberDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Application.Tests.Services;

public class FarmerStaffMemberServiceTests
{
    private readonly Mock<IFarmerStaffMemberRepository> _farmerStaffMemberRepository = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<FarmerStaffMemberService>> _logger = new();
    private readonly FarmerStaffMemberService _service;

    public FarmerStaffMemberServiceTests()
    {
        _service = new FarmerStaffMemberService(_farmerStaffMemberRepository.Object, _farmerProfileRepository.Object, _userRepository.Object, _logger.Object);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new FarmerProfile { Id = id, UserId = 1, FarmName = "F", Region = "Хатлон", District = "Бохтар", Village = "V", Address = "A", VerificationStatus = FarmerVerificationStatus.Verified });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new User { Id = id, Role = UserRole.Customer, FullName = "U", Email = "u@e.com", PhoneNumber = "1", PasswordHash = "h" });
        _farmerStaffMemberRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
    }

    private static FarmerStaffMember CreateMember(int id = 1, int farmerProfileId = 1, int userId = 1) => new()
    {
        Id = id,
        FarmerProfileId = farmerProfileId,
        UserId = userId,
        Permissions = StaffPermissions.ManageProducts,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static CreateFarmerStaffMemberDto ValidCreateDto(int farmerProfileId = 1, int userId = 1) => new()
    {
        FarmerProfileId = farmerProfileId,
        UserId = userId,
        Permissions = StaffPermissions.ManageProducts,
        IsActive = true
    };

    private static UpdateFarmerStaffMemberDto ValidUpdateDto(int id = 1, int farmerProfileId = 1, int userId = 1) => new()
    {
        Id = id,
        FarmerProfileId = farmerProfileId,
        UserId = userId,
        Permissions = StaffPermissions.ManageProducts,
        IsActive = true
    };

    // ---------- GetAllAsync ----------

    [Fact]
    public async Task GetAllAsync_MembersExist_ReturnsMappedDtos()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateMember(1), CreateMember(2, 1, 2)]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Count());
    }

    [Fact]
    public async Task GetAllAsync_RepositoryEmpty_ReturnsEmptyList()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task GetAllAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- GetByIdAsync ----------

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDtoWithCorrectFields()
    {
        var member = CreateMember(5);
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(member);

        var result = await _service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(member.Id, result.Data!.Id);
        Assert.Equal(member.Permissions, result.Data!.Permissions);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNotFound()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerStaffMember?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.GetByIdAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ValidData_AddsMemberAndReturnsOk()
    {
        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.True(result.IsSuccess);
        _farmerStaffMemberRepository.Verify(r => r.AddAsync(It.IsAny<FarmerStaffMember>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ZeroFarmerProfileId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.FarmerProfileId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.AddAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ZeroUserId_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.UserId = 0;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.AddAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidPermissionsBits_ReturnsValidationError()
    {
        var dto = ValidCreateDto();
        dto.Permissions = (StaffPermissions)16;

        var result = await _service.CreateAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.AddAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FarmerProfileNotFound_ReturnsNotFound()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.AddAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserNotFound_ReturnsNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.AddAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserAlreadyStaffMember_ReturnsConflict()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateMember(1, 1, 1)]);

        var result = await _service.CreateAsync(ValidCreateDto(1, 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.AddAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateAsync(ValidCreateDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_ValidData_UpdatesMemberAndReturnsOk()
    {
        var member = CreateMember(1);
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(member);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.True(result.IsSuccess);
        _farmerStaffMemberRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerStaffMember>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_MemberNotFound_ReturnsNotFound()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerStaffMember?)null);

        var result = await _service.UpdateAsync(999, ValidUpdateDto(999));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_FarmerProfileNotFound_ReturnsNotFound()
    {
        var member = CreateMember(1);
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(member);
        _farmerProfileRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerProfile?)null);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UserAlreadyStaffMemberOnAnotherRecord_ReturnsConflict()
    {
        var member = CreateMember(1, 1, 1);
        var other = CreateMember(2, 1, 2);
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(member);
        _farmerStaffMemberRepository.Setup(r => r.GetAllAsync()).ReturnsAsync([member, other]);

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1, 1, 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.UpdateAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateAsync(1, ValidUpdateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_ExistingMember_DeletesAndReturnsOk()
    {
        var member = CreateMember(1);
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(member);

        var result = await _service.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _farmerStaffMemberRepository.Verify(r => r.DeleteAsync(member), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MemberNotFound_ReturnsNotFound()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((FarmerStaffMember?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        _farmerStaffMemberRepository.Verify(r => r.DeleteAsync(It.IsAny<FarmerStaffMember>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsInternalServerError()
    {
        _farmerStaffMemberRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteAsync(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }
}
