using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.CustomerProfileDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ICustomerProfileService
{
    Task<Result<IEnumerable<GetCustomerProfileDto>>> GetAllAsync();
    Task<Result<GetCustomerProfileDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateCustomerProfileDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateCustomerProfileDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
