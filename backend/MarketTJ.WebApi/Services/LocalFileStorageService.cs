using MarketTJ.Application.Interfaces.Services;

namespace MarketTJ.WebApi.Services;

// Хранит файлы на диске в wwwroot/uploads/{subFolder}/{guid}{ext} и отдаёт их
// через уже подключённый app.UseStaticFiles() (см. Program.cs). Реализация
// живёт в WebApi, а не в Infrastructure, т.к. зависит от IWebHostEnvironment —
// тот же приём, что и у CurrentUserService (см. комментарий в
// ICurrentUserService про ASP.NET Core-specific концепции).
public class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    private const string UploadsRoot = "uploads";

    public async Task<string> SaveAsync(Stream content, string fileName, string subFolder)
    {
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var folderPath = Path.Combine(environment.ContentRootPath, "wwwroot", UploadsRoot, subFolder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, storedFileName);
        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream);

        return $"/{UploadsRoot}/{subFolder.Replace('\\', '/')}/{storedFileName}";
    }

    public void Delete(string? relativeUrl)
    {
        // Внешние ссылки (введённые вручную через CreateProductImageDto.ImageUrl,
        // не через UploadAsync) не трогаем — физического файла под ними у нас нет.
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith($"/{UploadsRoot}/", StringComparison.Ordinal))
            return;

        var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.Combine(environment.ContentRootPath, "wwwroot", relativePath);

        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
