using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Dto.ProductListingDto;
using MarketTJ.Application.Dto.ReportedListingDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MarketTJ.Application.Tests.Services;

public class AiAssistantServiceTests
{
    private readonly Mock<IProductListingRepository> _productListingRepository = new();
    private readonly Mock<IProductListingService> _productListingService = new();
    private readonly Mock<IFarmerProfileRepository> _farmerProfileRepository = new();
    private readonly Mock<IFarmerProfileService> _farmerProfileService = new();
    private readonly Mock<IReportedListingService> _reportedListingService = new();
    private readonly Mock<IAnalyticsService> _analyticsService = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<ICustomerProfileRepository> _customerProfileRepository = new();
    private readonly Mock<IDeliveryZoneRepository> _deliveryZoneRepository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<AiAssistantService>> _logger = new();

    private static Mock<HttpMessageHandler> MockHandler(HttpStatusCode statusCode, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode, Content = new StringContent(content) });
        return handler;
    }

    private static Mock<HttpMessageHandler> MockHandlerSequence(params (HttpStatusCode Status, string Body)[] responses)
    {
        var handler = new Mock<HttpMessageHandler>();
        var setup = handler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        foreach (var (status, body) in responses)
        {
            setup = setup.ReturnsAsync(new HttpResponseMessage { StatusCode = status, Content = new StringContent(body) });
        }
        return handler;
    }

    private AiAssistantService CreateService(Mock<HttpMessageHandler> handler, string? apiKey = "test-groq-key")
    {
        _configuration.Setup(c => c["Groq:ApiKey"]).Returns(apiKey);
        var httpClient = new HttpClient(handler.Object);
        return new AiAssistantService(
            httpClient,
            _productListingRepository.Object,
            _productListingService.Object,
            _farmerProfileRepository.Object,
            _farmerProfileService.Object,
            _reportedListingService.Object,
            _analyticsService.Object,
            _orderRepository.Object,
            _customerProfileRepository.Object,
            _deliveryZoneRepository.Object,
            _currentUser.Object,
            _configuration.Object,
            _logger.Object);
    }

    private static string GroqTextResponse(string content)
    {
        var body = new JsonObject
        {
            ["choices"] = new JsonArray
            {
                new JsonObject { ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = content } }
            }
        };
        return body.ToJsonString();
    }

    private static string GroqToolCallResponse(string toolCallId, string functionName, string argumentsJson)
    {
        var body = new JsonObject
        {
            ["choices"] = new JsonArray
            {
                new JsonObject
                {
                    ["message"] = new JsonObject
                    {
                        ["role"] = "assistant",
                        ["content"] = null,
                        ["tool_calls"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["id"] = toolCallId,
                                ["type"] = "function",
                                ["function"] = new JsonObject { ["name"] = functionName, ["arguments"] = argumentsJson }
                            }
                        }
                    }
                }
            }
        };
        return body.ToJsonString();
    }

    [Fact]
    public async Task AskAsync_MissingApiKey_ReturnsFailureWithoutCallingHttp()
    {
        var handler = MockHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler, apiKey: null);

        var result = await service.AskAsync("hello");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
        handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_CustomerNoToolCall_ParsesJsonFromMessageContent()
    {
        var body = GroqTextResponse("{\"intent\":\"none\",\"message\":\"Не понял вопрос\"}");
        var handler = MockHandler(HttpStatusCode.OK, body);
        var service = CreateService(handler);

        var result = await service.AskAsync("what?");

        Assert.True(result.IsSuccess);
        Assert.Equal("none", result.Data!.Intent);
        Assert.Equal("Не понял вопрос", result.Data.Message);
    }

    [Fact]
    public async Task AskAsync_ResponseWrappedInMarkdownFence_StripsFenceBeforeParsing()
    {
        var body = GroqTextResponse("```json\n{\"intent\":\"none\",\"message\":\"ok\"}\n```");
        var handler = MockHandler(HttpStatusCode.OK, body);
        var service = CreateService(handler);

        var result = await service.AskAsync("what?");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Data!.Message);
    }

    [Fact]
    public async Task AskAsync_SendsBearerAuthHeaderAndCorrectModel()
    {
        HttpRequestMessage? captured = null;
        JsonObject? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                captured = req;
                var text = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                capturedBody = JsonNode.Parse(text)!.AsObject();
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(GroqTextResponse("{\"intent\":\"none\",\"message\":\"ok\"}")) });
        var service = CreateService(handler, apiKey: "secret-groq-key");

        await service.AskAsync("tomatoes");

        Assert.Equal("https://api.groq.com/openai/v1/chat/completions", captured!.RequestUri!.ToString());
        Assert.Equal("Bearer", captured.Headers.Authorization!.Scheme);
        Assert.Equal("secret-groq-key", captured.Headers.Authorization!.Parameter);
        Assert.Equal("llama-3.3-70b-versatile", capturedBody!["model"]!.GetValue<string>());
        var messages = capturedBody["messages"]!.AsArray();
        Assert.Equal("system", messages[0]!["role"]!.GetValue<string>());
        Assert.Equal("user", messages[1]!["role"]!.GetValue<string>());
        Assert.Equal("tomatoes", messages[1]!["content"]!.GetValue<string>());
    }

    [Fact]
    public async Task AskAsync_CustomerSearchProductsToolCall_ExecutesSearchAndSendsToolResultBack()
    {
        _productListingRepository.Setup(r => r.SearchAsync("tomato")).ReturnsAsync(
        [
            new ProductListing { Id = 1, Title = "Помидор", RetailPricePerKg = 12 }
        ]);

        var first = GroqToolCallResponse("call_1", "search_products", "{\"query\":\"tomato\"}");
        var second = GroqTextResponse("{\"intent\":\"product\",\"productId\":1,\"message\":\"Нашёл помидоры\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?");

        Assert.True(result.IsSuccess);
        Assert.Equal("product", result.Data!.Intent);
        Assert.Equal(1, result.Data.ProductId);
        _productListingRepository.Verify(r => r.SearchAsync("tomato"), Times.Once);
        handler.Protected().Verify("SendAsync", Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_SecondRequest_IncludesAssistantToolCallAndToolResultMessages()
    {
        _productListingRepository.Setup(r => r.SearchAsync(It.IsAny<string>())).ReturnsAsync([]);

        var capturedBodies = new List<JsonObject>();
        var handler = new Mock<HttpMessageHandler>();
        var responses = new Queue<string>(new[]
        {
            GroqToolCallResponse("call_abc", "search_products", "{\"query\":\"tomato\"}"),
            GroqTextResponse("{\"intent\":\"none\",\"message\":\"Ничего не найдено\"}")
        });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                var text = await req.Content!.ReadAsStringAsync();
                capturedBodies.Add(JsonNode.Parse(text)!.AsObject());
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responses.Dequeue()) };
            });
        var service = CreateService(handler);

        await service.AskAsync("tomatoes?");

        Assert.Equal(2, capturedBodies.Count);
        var secondMessages = capturedBodies[1]["messages"]!.AsArray();
        // system, user, assistant(tool_calls), tool(result)
        Assert.Equal(4, secondMessages.Count);
        Assert.Equal("assistant", secondMessages[2]!["role"]!.GetValue<string>());
        Assert.NotNull(secondMessages[2]!["tool_calls"]);
        Assert.Equal("tool", secondMessages[3]!["role"]!.GetValue<string>());
        Assert.Equal("call_abc", secondMessages[3]!["tool_call_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task AskAsync_CustomerGetOrderStatus_FiltersByOwnCustomerProfileAndOrderNumber()
    {
        _currentUser.Setup(c => c.UserId).Returns(42);
        _customerProfileRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(new CustomerProfile { Id = 7, UserId = 42 });
        _orderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Order { Id = 1, OrderNumber = "ORD-001", CustomerId = 7, Status = Domain.Enums.OrderStatus.InDelivery, TotalAmount = 150, DeliveryAddress = "ул. Рудаки 1" },
            new Order { Id = 2, OrderNumber = "ORD-002", CustomerId = 999, Status = Domain.Enums.OrderStatus.Completed, TotalAmount = 50, DeliveryAddress = "elsewhere" }
        ]);

        var first = GroqToolCallResponse("call_4", "get_order_status", "{\"orderNumber\":\"ORD-001\"}");
        var second = GroqTextResponse("{\"intent\":\"orders\",\"message\":\"Ваш заказ ORD-001 в пути\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("где мой заказ ORD-001?");

        Assert.True(result.IsSuccess);
        Assert.Equal("orders", result.Data!.Intent);
        _orderRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_CustomerGetDeliveryInfo_ReturnsOnlyActiveZones()
    {
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new DeliveryZone { Id = 1, Region = "Душанбе", District = "Сино", BasePrice = 20, PricePerKm = 2, IsActive = true },
            new DeliveryZone { Id = 2, Region = "Хатлон", District = "Бохтар", BasePrice = 15, PricePerKm = 1.5m, IsActive = false }
        ]);

        var first = GroqToolCallResponse("call_5", "get_delivery_info", "{}");
        var second = GroqTextResponse("{\"intent\":\"none\",\"message\":\"Доставка по Душанбе стоит от 20 сомони\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("сколько стоит доставка?");

        Assert.True(result.IsSuccess);
        _deliveryZoneRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_FarmerProposeUpdateListing_ReturnsActionPendingWithoutSecondHttpCall()
    {
        _currentUser.Setup(c => c.Role).Returns("Farmer");
        _productListingService.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(Result<GetProductListingDto?>.Ok(new GetProductListingDto { Id = 5, Title = "Картофель" }));

        var body = GroqToolCallResponse("call_2", "propose_update_listing", "{\"listingId\":5,\"field\":\"price\",\"value\":\"20\"}");
        var handler = MockHandler(HttpStatusCode.OK, body);
        var service = CreateService(handler);

        var result = await service.AskAsync("подними цену на картофель до 20");

        Assert.True(result.IsSuccess);
        Assert.Equal("action_pending", result.Data!.Intent);
        Assert.Equal("update_listing", result.Data.Action!.Type);
        Assert.Equal("5", result.Data.Action.Params["listingId"]);
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_AdminProposeResolveReport_ReturnsActionPendingWithoutSecondHttpCall()
    {
        _currentUser.Setup(c => c.Role).Returns("Admin");
        _reportedListingService.Setup(s => s.GetByIdAsync(9)).ReturnsAsync(Result<GetReportedListingDto?>.Ok(new GetReportedListingDto { Id = 9, ProductListingId = 3 }));

        var body = GroqToolCallResponse("call_3", "propose_resolve_report", "{\"reportId\":9,\"resolution\":\"Dismissed\"}");
        var handler = MockHandler(HttpStatusCode.OK, body);
        var service = CreateService(handler);

        var result = await service.AskAsync("отклони жалобу 9");

        Assert.True(result.IsSuccess);
        Assert.Equal("action_pending", result.Data!.Intent);
        Assert.Equal("resolve_report", result.Data.Action!.Type);
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_GroqApiError_ReturnsGenericFailure()
    {
        var handler = MockHandler(HttpStatusCode.TooManyRequests, "{\"error\":{\"message\":\"rate limited\"}}");
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    [Fact]
    public async Task AskAsync_NoTextAndNoToolCall_ReturnsFailure()
    {
        var handler = MockHandler(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":null}}]}""");
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    [Fact]
    public async Task AskAsync_ToolUseFailed_RetriesOnceAndSucceeds()
    {
        var toolUseFailedBody = """{"error":{"message":"Failed to call a function.","type":"invalid_request_error","code":"tool_use_failed"}}""";
        var successBody = GroqTextResponse("{\"intent\":\"none\",\"message\":\"ok after retry\"}");
        var handler = MockHandlerSequence((HttpStatusCode.BadRequest, toolUseFailedBody), (HttpStatusCode.OK, successBody));
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok after retry", result.Data!.Message);
        handler.Protected().Verify("SendAsync", Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_ToolUseFailedWithAnswerInFailedGeneration_SalvagesWithoutRetrying()
    {
        // Модель иногда пишет в failed_generation и неудавшийся текстовый
        // вызов функции, и корректный финальный JSON следом за ним одним куском.
        var failedGenerationBody = """
            {"error":{"message":"Failed to call a function.","type":"invalid_request_error","code":"tool_use_failed","failed_generation":"<function=search_products>{\"query\": \"tomato\"}\n\n{\"intent\": \"category\", \"productId\": null, \"categoryId\": null, \"message\": \"Yes, we have fresh tomatoes from several farmers.\"}"}}
            """;
        var handler = MockHandler(HttpStatusCode.BadRequest, failedGenerationBody);
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?");

        Assert.True(result.IsSuccess);
        Assert.Equal("category", result.Data!.Intent);
        Assert.Equal("Yes, we have fresh tomatoes from several farmers.", result.Data.Message);
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_ToolUseFailedTwiceWithNoSalvage_FallsBackToNoToolsRequest()
    {
        var toolUseFailedBody = """{"error":{"message":"Failed to call a function.","type":"invalid_request_error","code":"tool_use_failed"}}""";
        var fallbackBody = GroqTextResponse("{\"intent\":\"none\",\"message\":\"answered without tools\"}");
        var handler = MockHandlerSequence(
            (HttpStatusCode.BadRequest, toolUseFailedBody),
            (HttpStatusCode.BadRequest, toolUseFailedBody),
            (HttpStatusCode.OK, fallbackBody));
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?");

        Assert.True(result.IsSuccess);
        Assert.Equal("answered without tools", result.Data!.Message);
        handler.Protected().Verify("SendAsync", Times.Exactly(3), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_ToolUseFailedThreeTimes_ReturnsGenericFailure()
    {
        var toolUseFailedBody = """{"error":{"message":"Failed to call a function.","type":"invalid_request_error","code":"tool_use_failed"}}""";
        var handler = MockHandlerSequence(
            (HttpStatusCode.BadRequest, toolUseFailedBody),
            (HttpStatusCode.BadRequest, toolUseFailedBody),
            (HttpStatusCode.BadRequest, toolUseFailedBody));
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
        handler.Protected().Verify("SendAsync", Times.Exactly(3), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_NonToolUseFailedBadRequest_DoesNotRetry()
    {
        var otherErrorBody = """{"error":{"message":"invalid model","type":"invalid_request_error","code":"model_not_found"}}""";
        var handler = MockHandler(HttpStatusCode.BadRequest, otherErrorBody);
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?");

        Assert.False(result.IsSuccess);
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteActionAsync_ResolveReport_NonAdmin_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.Role).Returns("Customer");
        var handler = MockHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler);

        var result = await service.ExecuteActionAsync(new ExecuteAssistantActionDto
        {
            Type = "resolve_report",
            Params = new Dictionary<string, string> { ["reportId"] = "1", ["resolution"] = "Dismissed" }
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task ExecuteActionAsync_UnknownType_ReturnsBadRequest()
    {
        var handler = MockHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler);

        var result = await service.ExecuteActionAsync(new ExecuteAssistantActionDto { Type = "delete_everything" });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.BadRequest, result.ErrorType);
    }
}
