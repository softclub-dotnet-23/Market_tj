using System.Text.Json.Nodes;
using MarketTJ.Application.Common;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// Серверное геокодирование (курьер: Address/Region/District → Latitude/
// Longitude при регистрации/обновлении профиля; заказ: адрес доставки →
// координаты, лениво при первом запросе списка доступных курьеров) — по
// прямому запросу пользователя (2026-08-05), для расчёта реального
// расстояния (см. GeoDistance.HaversineKm) вместо сравнения текста региона/
// района. GoogleMaps:ApiKey — отдельный, серверный ключ (не путать с
// VITE_GOOGLE_MAPS_API_KEY фронтенда, который ограничен по HTTP-referrer и
// поэтому не годится для вызовов с бэкенда) — не хранится в appsettings.json,
// та же схема, что и Groq:ApiKey (user-secrets/переменные окружения).
public class GoogleGeocodingService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GoogleGeocodingService> logger) : IGoogleGeocodingService
{
    private const string ApiUrl = "https://maps.googleapis.com/maps/api/geocode/json";

    public async Task<Result<(double Latitude, double Longitude)>> GeocodeAsync(string address)
    {
        var apiKey = configuration["GoogleMaps:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogError("GoogleMaps:ApiKey не задан (appsettings.json / User Secrets)");
            return Result<(double, double)>.Fail("Геокодирование недоступно", ErrorType.InternalServerError);
        }

        try
        {
            var url = $"{ApiUrl}?address={Uri.EscapeDataString(address)}&key={apiKey}";
            using var response = await httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google Geocoding API вернул {StatusCode}: {Body}", response.StatusCode, body);
                return Result<(double, double)>.Fail("Не удалось геокодировать адрес", ErrorType.InternalServerError);
            }

            var json = JsonNode.Parse(body);
            var status = json?["status"]?.GetValue<string>();
            var results = json?["results"]?.AsArray();

            if (status != "OK" || results is null || results.Count == 0)
            {
                logger.LogWarning("Google Geocoding API status={Status} для адреса {Address}", status, address);
                return Result<(double, double)>.Fail("Адрес не найден", ErrorType.Validation);
            }

            var location = results[0]!["geometry"]!["location"]!;
            var lat = location["lat"]!.GetValue<double>();
            var lng = location["lng"]!.GetValue<double>();

            return Result<(double, double)>.Ok((lat, lng));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обращении к Google Geocoding API для адреса {Address}", address);
            return Result<(double, double)>.Fail("Не удалось геокодировать адрес", ErrorType.InternalServerError);
        }
    }
}
