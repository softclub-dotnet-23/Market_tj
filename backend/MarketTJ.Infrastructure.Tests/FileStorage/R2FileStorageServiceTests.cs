using Amazon.S3;
using Amazon.S3.Model;
using MarketTJ.Infrastructure.FileStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketTJ.Infrastructure.Tests.FileStorage;

public class R2FileStorageServiceTests
{
    private readonly Mock<IAmazonS3> _s3Client = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<R2FileStorageService>> _logger = new();
    private readonly R2FileStorageService _service;

    public R2FileStorageServiceTests()
    {
        _configuration.Setup(c => c["R2:BucketName"]).Returns("market-tj-uploads");
        _configuration.Setup(c => c["R2:PublicUrl"]).Returns("https://pub-abc123.r2.dev");
        _service = new R2FileStorageService(_s3Client.Object, _configuration.Object, _logger.Object);
    }

    private static Stream SampleStream() => new MemoryStream([1, 2, 3]);

    // === SaveAsync ===

    [Fact]
    public async Task SaveAsync_ValidRequest_UploadsToConfiguredBucketAndReturnsPublicUrl()
    {
        PutObjectRequest? captured = null;
        _s3Client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new PutObjectResponse());

        var url = await _service.SaveAsync(SampleStream(), "photo.jpg", "avatars/3");

        Assert.Equal("market-tj-uploads", captured!.BucketName);
        Assert.StartsWith("avatars/3/", captured.Key);
        Assert.EndsWith(".jpg", captured.Key);
        Assert.StartsWith("https://pub-abc123.r2.dev/avatars/3/", url);
        Assert.EndsWith(".jpg", url);
    }

    [Fact]
    public async Task SaveAsync_GeneratesUniqueKeyPerCall_NoCollisionsBetweenUploads()
    {
        _s3Client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        var url1 = await _service.SaveAsync(SampleStream(), "a.jpg", "listings/1");
        var url2 = await _service.SaveAsync(SampleStream(), "a.jpg", "listings/1");

        Assert.NotEqual(url1, url2);
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.JPEG", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("passport.pdf", "application/pdf")]
    [InlineData("file.unknownext", "application/octet-stream")]
    public async Task SaveAsync_SetsContentTypeFromExtension(string fileName, string expectedContentType)
    {
        PutObjectRequest? captured = null;
        _s3Client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new PutObjectResponse());

        await _service.SaveAsync(SampleStream(), fileName, "documents/1");

        Assert.Equal(expectedContentType, captured!.ContentType);
    }

    [Fact]
    public async Task SaveAsync_MissingBucketNameConfig_ThrowsInvalidOperationExceptionWithoutCallingS3()
    {
        _configuration.Setup(c => c["R2:BucketName"]).Returns((string?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(SampleStream(), "a.jpg", "avatars/3"));
        _s3Client.Verify(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_MissingPublicUrlConfig_ThrowsInvalidOperationExceptionWithoutCallingS3()
    {
        _configuration.Setup(c => c["R2:PublicUrl"]).Returns((string?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(SampleStream(), "a.jpg", "avatars/3"));
        _s3Client.Verify(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_S3ThrowsAmazonS3Exception_WrapsInFriendlyInvalidOperationException()
    {
        _s3Client.Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Access Denied"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(SampleStream(), "a.jpg", "avatars/3"));
        Assert.Contains("Access Denied", ex.Message);
    }

    // === Delete ===

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Delete_NullOrEmptyUrl_DoesNothing(string? url)
    {
        _service.Delete(url);
        // Fire-and-forget: даём внутренней задаче шанс выполниться, чтобы
        // убедиться, что она действительно ничего не вызвала, а не просто
        // "ещё не успела".
        await Task.Delay(20);

        _s3Client.Verify(c => c.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_UrlFromDifferentOrigin_DoesNothing()
    {
        _service.Delete("https://someone-elses-cdn.example.com/avatars/3/photo.jpg");
        await Task.Delay(20);

        _s3Client.Verify(c => c.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_OwnBucketUrl_DeletesUsingObjectKeyDerivedFromUrl()
    {
        _s3Client.Setup(c => c.DeleteObjectAsync("market-tj-uploads", "avatars/3/abc123.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectResponse());

        _service.Delete("https://pub-abc123.r2.dev/avatars/3/abc123.jpg");
        await Task.Delay(20);

        _s3Client.Verify(c => c.DeleteObjectAsync("market-tj-uploads", "avatars/3/abc123.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_S3Throws_DoesNotPropagateException()
    {
        // Delete() — синхронный void, вызывающий код (например
        // UserService.UploadAvatarAsync) не должен упасть из-за сбоя
        // удаления СТАРОГО файла при замене на новый.
        _s3Client.Setup(c => c.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Not Found"));

        var exception = Record.Exception(() => _service.Delete("https://pub-abc123.r2.dev/avatars/3/abc123.jpg"));
        await Task.Delay(20);

        Assert.Null(exception);
    }
}
