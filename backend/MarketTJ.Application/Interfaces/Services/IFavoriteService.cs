using MarketTJ.Application.Results;
using MarketTJ.Application.Dto.FavoriteDto;

namespace MarketTJ.Application.Interfaces.Services;

public interface IFavoriteService
{
    Task<Result<IEnumerable<GetFavoriteDto>>> GetAllAsync();
    Task<Result<GetFavoriteDto?>> GetByIdAsync(int id);
    Task<Result<string>> CreateAsync(CreateFavoriteDto dto);
    Task<Result<string>> UpdateAsync(int id, UpdateFavoriteDto dto);
    Task<Result<string>> DeleteAsync(int id);
}
