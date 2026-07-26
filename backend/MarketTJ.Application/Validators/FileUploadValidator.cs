using MarketTJ.Application.Common;
using MarketTJ.Application.Results;

namespace MarketTJ.Application.Validators;

// Общее правило для загрузки файлов-изображений (аватар пользователя, фото
// объявления) — те же разрешённые форматы, что и в ProductImageValidator
// (раздел 8.8 ТЗ), плюс ограничение размера, которого раньше не было (там
// ImageUrl была просто строкой — размер файла физически не мог прилететь).
public static class FileUploadValidator
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxSizeBytes = 5 * 1024 * 1024;

    public static Result<string>? ValidateImage(string fileName, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result<string>.Fail("Файл обязателен", ErrorType.Validation);

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return Result<string>.Fail("Разрешены только форматы: jpg, jpeg, png, webp", ErrorType.Validation);

        if (sizeBytes <= 0)
            return Result<string>.Fail("Файл пустой", ErrorType.Validation);

        if (sizeBytes > MaxSizeBytes)
            return Result<string>.Fail("Максимальный размер файла — 5 МБ", ErrorType.Validation);

        return null;
    }
}
