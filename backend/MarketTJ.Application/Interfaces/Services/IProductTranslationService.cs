namespace MarketTJ.Application.Interfaces.Services;

public record ProductTranslationInput(
    string? TitleRu, string? TitleTj, string? TitleEn,
    string? DescriptionRu, string? DescriptionTj, string? DescriptionEn);

public record ProductTranslationOutput(
    string? TitleRu, string? TitleTj, string? TitleEn,
    string? DescriptionRu, string? DescriptionTj, string? DescriptionEn);

public interface IProductTranslationService
{
    // Возвращает недостающие поля, переведённые с того языка(ов), что
    // фермер реально заполнил — уже заполненные поля не трогает. Никогда не
    // бросает исключение наружу (см. GroqProductTranslationService) — при
    // недоступности/ошибке Groq возвращает null для непереведённых полей,
    // вызывающий код (ProductListingService) не блокирует сохранение
    // объявления из-за этого.
    Task<ProductTranslationOutput> TranslateMissingAsync(ProductTranslationInput input);
}
