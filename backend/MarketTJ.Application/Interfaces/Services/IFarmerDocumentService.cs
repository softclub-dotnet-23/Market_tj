using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.FarmerDocumentDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IFarmerDocumentService
{
    Task<Result<IEnumerable<GetFarmerDocumentDto>>> GetAllAsync();
    Task<Result<GetFarmerDocumentDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateFarmerDocumentDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateFarmerDocumentDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
