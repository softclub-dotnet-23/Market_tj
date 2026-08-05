namespace MarketTJ.Domain.Constants;

// Единица измерения "кг" — то же значение, что и в фиксированном наборе
// UNIT_OPTIONS на фронтенде (Frontend/src/pages/FarmerProducts.tsx) и в
// locales/{ru,tj,en}/product.json:units.
public static class OrderQuantityConstants
{
    public const string WeightUnit = "кг";
    public const decimal MinimumWeightOrderQuantity = 50m;
}
