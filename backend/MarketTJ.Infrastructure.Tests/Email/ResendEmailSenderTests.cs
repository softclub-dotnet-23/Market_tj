using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using MarketTJ.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MarketTJ.Infrastructure.Tests.Email;

public class ResendEmailSenderTests
{
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<ResendEmailSender>> _logger = new();

    private static Mock<HttpMessageHandler> MockHandler(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode, Content = new StringContent(content) });
        return handler;
    }

    private ResendEmailSender CreateSender(Mock<HttpMessageHandler> handler, string? apiKey = "test-api-key", string? fromEmail = "noreply@market.tj")
    {
        _configuration.Setup(c => c["Resend:ApiKey"]).Returns(apiKey);
        _configuration.Setup(c => c["Resend:FromEmail"]).Returns(fromEmail);
        var httpClient = new HttpClient(handler.Object);
        return new ResendEmailSender(httpClient, _configuration.Object, _logger.Object);
    }

    [Fact]
    public async Task SendAsync_Success_CompletesWithoutThrowing()
    {
        var handler = MockHandler(HttpStatusCode.OK, "{\"id\":\"abc123\"}");
        var sender = CreateSender(handler);

        await sender.SendAsync("user@example.com", "Subject", "<p>Body</p>");

        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_SendsBearerTokenAndCorrectPayload()
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        AuthenticationHeaderValue? capturedAuth = null;
        string? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedMethod = req.Method;
                capturedUri = req.RequestUri;
                capturedAuth = req.Headers.Authorization;
                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });
        var sender = CreateSender(handler, apiKey: "secret-key", fromEmail: "noreply@market.tj");

        await sender.SendAsync("user@example.com", "Код подтверждения", "<p>123456</p>");

        Assert.Equal(HttpMethod.Post, capturedMethod);
        Assert.Equal("https://api.resend.com/emails", capturedUri!.ToString());
        Assert.Equal("Bearer", capturedAuth!.Scheme);
        Assert.Equal("secret-key", capturedAuth.Parameter);

        var payload = JsonNode.Parse(capturedBody!)!;
        Assert.Equal("noreply@market.tj", payload["from"]!.GetValue<string>());
        Assert.Equal("user@example.com", payload["to"]![0]!.GetValue<string>());
        Assert.Equal("Код подтверждения", payload["subject"]!.GetValue<string>());
        Assert.Equal("<p>123456</p>", payload["html"]!.GetValue<string>());
    }

    [Fact]
    public async Task SendAsync_MissingApiKey_ThrowsWithoutCallingHttp()
    {
        var handler = MockHandler(HttpStatusCode.OK, "{}");
        var sender = CreateSender(handler, apiKey: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("user@example.com", "Subject", "Body"));

        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_MissingFromEmail_ThrowsWithoutCallingHttp()
    {
        var handler = MockHandler(HttpStatusCode.OK, "{}");
        var sender = CreateSender(handler, fromEmail: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("user@example.com", "Subject", "Body"));

        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_Unauthorized_ThrowsWithApiKeyMessage()
    {
        var handler = MockHandler(HttpStatusCode.Unauthorized, "{\"message\":\"Invalid API key\"}");
        var sender = CreateSender(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("user@example.com", "Subject", "Body"));
        Assert.Contains("API-ключ", ex.Message);
    }

    [Fact]
    public async Task SendAsync_TooManyRequests_ThrowsWithRateLimitMessage()
    {
        var handler = MockHandler(HttpStatusCode.TooManyRequests, "{\"message\":\"Rate limit exceeded\"}");
        var sender = CreateSender(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("user@example.com", "Subject", "Body"));
        Assert.Contains("лимит", ex.Message);
    }

    [Fact]
    public async Task SendAsync_UnprocessableEntity_ThrowsWithValidationMessage()
    {
        var handler = MockHandler(HttpStatusCode.UnprocessableEntity, "{\"message\":\"Invalid `to` field\"}");
        var sender = CreateSender(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("not-an-email", "Subject", "Body"));
        Assert.Contains("некорректные данные", ex.Message);
    }

    [Fact]
    public async Task SendAsync_Forbidden_ThrowsWithDomainMessage()
    {
        var handler = MockHandler(HttpStatusCode.Forbidden, "{\"message\":\"You can only send testing emails to your own email address\"}");
        var sender = CreateSender(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("someone-else@example.com", "Subject", "Body"));
        Assert.Contains("домен", ex.Message);
    }

    [Fact]
    public async Task SendAsync_UnexpectedStatusCode_ThrowsWithStatusCodeInMessage()
    {
        var handler = MockHandler(HttpStatusCode.InternalServerError, "{\"message\":\"Something went wrong\"}");
        var sender = CreateSender(handler);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync("user@example.com", "Subject", "Body"));
        Assert.Contains("500", ex.Message);
    }
}
