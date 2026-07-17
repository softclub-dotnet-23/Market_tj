using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.PaymentDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<Result<IEnumerable<GetPaymentDto>>> GetAllAsync();
    Task<Result<GetPaymentDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreatePaymentDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdatePaymentDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
