using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.CommissionDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface ICommissionService
{
    Task<Result<IEnumerable<GetCommissionDto>>> GetAllAsync();
    Task<Result<GetCommissionDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateCommissionDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateCommissionDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
