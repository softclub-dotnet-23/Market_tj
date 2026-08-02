using Amazon.S3;
using Amazon.S3.Model;
using MarketTJ.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Infrastructure.FileStorage;

// Cloudflare R2 (S3-совместимое хранилище) вместо локального диска —
// Railway не даёт backend'у persistent volume (в отличие от Postgres/Redis),
// поэтому всё, что LocalFileStorageService писал в wwwroot/uploads,
// пропадало при каждом редеплое (новый контейнер = чистая файловая
// система). IAmazonS3 внедряется через DI (регистрируется в Program.cs
// с кастомным ServiceURL на *.r2.cloudflarestorage.com), а не создаётся
// здесь напрямую — так клиент можно замокать в юнит-тестах и переиспользовать
// как singleton (AWS SDK клиенты потокобезопасны и рассчитаны на переиспользование).
public class R2FileStorageService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<R2FileStorageService> logger) : IFileStorageService
{
    public async Task<string> SaveAsync(Stream content, string fileName, string subFolder)
    {
        var bucketName = configuration["R2:BucketName"];
        var publicUrl = configuration["R2:PublicUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(publicUrl))
            throw new InvalidOperationException("R2 не настроен (R2:BucketName/R2:PublicUrl в конфигурации/переменных окружения)");

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        // Та же структура ключей, что и у LocalFileStorageService (subFolder/
        // /storedFileName) — существующие вызовы (avatars/{userId}, listings/
        // {productListingId}, documents/{farmerProfileId}, chat/{conversationId})
        // переносятся без изменений в сервисах, которые их формируют.
        var key = $"{subFolder.Replace('\\', '/')}/{storedFileName}";

        try
        {
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = content,
                ContentType = ResolveContentType(extension),
                // Владелец потока — вызывающий код (контроллер/сервис), не
                // AWS SDK — тот не должен закрывать чужой Stream за нас.
                AutoCloseStream = false,
                DisablePayloadSigning = true
            });
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "Не удалось загрузить файл {Key} в R2 (bucket {Bucket})", key, bucketName);
            throw new InvalidOperationException($"Не удалось загрузить файл в облачное хранилище: {ex.Message}", ex);
        }

        // Абсолютный публичный URL (не относительный путь, как у Local) —
        // фронтенд (resolveMediaUrl в lib/api.ts) уже умеет отдавать такие
        // URL как есть, без дополнительной обработки: path.startsWith("http")
        // ? path : ... — изменений на фронтенде не требуется.
        return $"{publicUrl}/{key}";
    }

    public void Delete(string? relativeUrl)
    {
        var publicUrl = configuration["R2:PublicUrl"]?.TrimEnd('/');
        var bucketName = configuration["R2:BucketName"];
        if (string.IsNullOrWhiteSpace(relativeUrl) || string.IsNullOrWhiteSpace(publicUrl) || string.IsNullOrWhiteSpace(bucketName))
            return;

        // Внешние ссылки (не из нашего бакета) не трогаем — та же защита,
        // что и в LocalFileStorageService для "чужих" ImageUrl.
        if (!relativeUrl.StartsWith($"{publicUrl}/", StringComparison.Ordinal))
            return;

        var key = relativeUrl[(publicUrl.Length + 1)..];

        // Delete() в IFileStorageService — синхронный void (вызывающий код,
        // например UserService.UploadAvatarAsync, не ждёт удаления старого
        // файла перед ответом пользователю) — в отличие от File.Delete на
        // локальном диске, вызов R2 идёт по сети, поэтому синхронно
        // дождаться результата здесь означало бы блокировать поток запроса.
        // "Fire-and-forget" с логированием ошибки — то же итоговое
        // поведение по контракту (старый файл в лучшем случае удаляется,
        // ошибка не мешает основной операции), но не добавляет сетевую
        // задержку в горячий путь замены аватарки/фото.
        _ = DeleteInternalAsync(bucketName, key);
    }

    private async Task DeleteInternalAsync(string bucketName, string key)
    {
        try
        {
            await s3Client.DeleteObjectAsync(bucketName, key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось удалить файл {Key} из R2 (bucket {Bucket})", key, bucketName);
        }
    }

    // Без явного ContentType S3-совместимые хранилища отдают
    // application/octet-stream — браузер скачивал бы аватарки/фото товаров
    // вместо того, чтобы показать их через <img>.
    private static string ResolveContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };
}
