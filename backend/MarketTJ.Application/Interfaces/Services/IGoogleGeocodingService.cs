using MarketTJ.Application.Results;

namespace MarketTJ.Application.Interfaces.Services;

public interface IGoogleGeocodingService
{
    Task<Result<(double Latitude, double Longitude)>> GeocodeAsync(string address);
}
