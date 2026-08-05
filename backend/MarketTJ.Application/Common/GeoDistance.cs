namespace MarketTJ.Application.Common;

// Haversine ("по прямой", не по дорогам) вместо Google Distance Matrix API —
// по прямому запросу пользователя (2026-08-05, раздел "выбор курьера по
// карте в радиусе 40 км") ограничение сформулировано именно как прямое
// расстояние ("≤ 40 км"), не время/расстояние в пути. Haversine считается
// мгновенно и без дополнительных вызовов/квоты внешнего API — Distance
// Matrix добавил бы задержку и стоимость ради точности, которая здесь не
// требовалась.
public static class GeoDistance
{
    private const double EarthRadiusKm = 6371.0;

    public static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
