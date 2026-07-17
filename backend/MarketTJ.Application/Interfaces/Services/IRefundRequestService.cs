using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.RefundRequestDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IRefundRequestService
{
    Task<Result<IEnumerable<GetRefundRequestDto>>> GetAllAsync();
    Task<Result<GetRefundRequestDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateRefundRequestDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateRefundRequestDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
