using MarketTJ.Application.Common;
using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.RefundRequestDto;
using MarketTJ.Domain.Enums;

namespace MarketTJ.Application.Interfaces.Services;

public interface IRefundRequestService
{
    Task<Result<IEnumerable<GetRefundRequestDto>>> GetAllAsync();
    Task<Result<GetRefundRequestDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateRefundRequestDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateRefundRequestDto dto);
    Task<Result<string>> DeleteAsync(int id);

    Task<Result<PagedResult<GetRefundRequestDto>>> GetPagedAsync(PagedRequest request, RefundStatus? status);
    Task<Result<string>> ApproveAsync(int id, int adminId);
    Task<Result<string>> RejectAsync(int id, int adminId);
}
