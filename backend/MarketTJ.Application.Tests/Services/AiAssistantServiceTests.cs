using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Dto.CommissionDto;
using MarketTJ.Application.Dto.CourierProfileDto;
using MarketTJ.Application.Dto.FarmerDocumentDto;
using MarketTJ.Application.Dto.FarmerStaffMemberDto;
using MarketTJ.Application.Dto.FavoriteDto;
using MarketTJ.Application.Dto.OrderDto;
using MarketTJ.Application.Dto.ProductListingDto;
using MarketTJ.Application.Dto.ReportedListingDto;
using MarketTJ.Application.Dto.ReviewDto;
using MarketTJ.Application.Dto.UserDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Application.Services;
using MarketTJ.Domain.Entities;
using MarketTJ.Domain.Enums;
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
    private readonly Mock<IOrderService> _orderService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ICourierProfileService> _courierProfileService = new();
    private readonly Mock<ICommissionService> _commissionService = new();
    private readonly Mock<IFarmerDocumentService> _farmerDocumentService = new();
    private readonly Mock<IFarmerStaffMemberService> _farmerStaffMemberService = new();
    private readonly Mock<IFavoriteService> _favoriteService = new();
    private readonly Mock<IReviewService> _reviewService = new();
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
            _orderService.Object,
            _userService.Object,
            _courierProfileService.Object,
            _commissionService.Object,
            _farmerDocumentService.Object,
            _farmerStaffMemberService.Object,
            _favoriteService.Object,
            _reviewService.Object,
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

        var result = await service.AskAsync("hello", null);

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

        var result = await service.AskAsync("what?", null);

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

        var result = await service.AskAsync("what?", null);

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

        await service.AskAsync("tomatoes", null);

        Assert.Equal("https://api.groq.com/openai/v1/chat/completions", captured!.RequestUri!.ToString());
        Assert.Equal("Bearer", captured.Headers.Authorization!.Scheme);
        Assert.Equal("secret-groq-key", captured.Headers.Authorization!.Parameter);
        Assert.Equal("llama-3.3-70b-versatile", capturedBody!["model"]!.GetValue<string>());
        Assert.Equal(0.3, capturedBody["temperature"]!.GetValue<double>());
        var messages = capturedBody["messages"]!.AsArray();
        Assert.Equal("system", messages[0]!["role"]!.GetValue<string>());
        Assert.Equal("user", messages[1]!["role"]!.GetValue<string>());
        Assert.Equal("tomatoes", messages[1]!["content"]!.GetValue<string>());
    }

    [Fact]
    public async Task AskAsync_WithHistory_IncludesPriorTurnsBeforeCurrentMessage()
    {
        JsonObject? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                var text = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                capturedBody = JsonNode.Parse(text)!.AsObject();
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(GroqTextResponse("{\"intent\":\"none\",\"message\":\"ok\"}")) });
        var service = CreateService(handler);

        var history = new List<AssistantHistoryMessageDto>
        {
            new() { Role = "user", Text = "статус заказа ORD-001?" },
            new() { Role = "assistant", Text = "Заказ ORD-001 в пути." }
        };

        await service.AskAsync("а когда придёт?", history);

        var messages = capturedBody!["messages"]!.AsArray();
        // system, history[0], history[1], current user message
        Assert.Equal(4, messages.Count);
        Assert.Equal("user", messages[1]!["role"]!.GetValue<string>());
        Assert.Equal("статус заказа ORD-001?", messages[1]!["content"]!.GetValue<string>());
        Assert.Equal("assistant", messages[2]!["role"]!.GetValue<string>());
        Assert.Equal("Заказ ORD-001 в пути.", messages[2]!["content"]!.GetValue<string>());
        Assert.Equal("user", messages[3]!["role"]!.GetValue<string>());
        Assert.Equal("а когда придёт?", messages[3]!["content"]!.GetValue<string>());
    }

    [Fact]
    public async Task AskAsync_WithLongHistory_TruncatesToLastTenMessages()
    {
        JsonObject? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                var text = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                capturedBody = JsonNode.Parse(text)!.AsObject();
            })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(GroqTextResponse("{\"intent\":\"none\",\"message\":\"ok\"}")) });
        var service = CreateService(handler);

        var history = Enumerable.Range(1, 20)
            .Select(i => new AssistantHistoryMessageDto { Role = i % 2 == 0 ? "assistant" : "user", Text = $"msg{i}" })
            .ToList();

        await service.AskAsync("current", history);

        var messages = capturedBody!["messages"]!.AsArray();
        // system + last 10 history messages + current
        Assert.Equal(12, messages.Count);
        Assert.Equal("msg11", messages[1]!["content"]!.GetValue<string>());
        Assert.Equal("msg20", messages[10]!["content"]!.GetValue<string>());
        Assert.Equal("current", messages[11]!["content"]!.GetValue<string>());
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

        var result = await service.AskAsync("tomatoes?", null);

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

        await service.AskAsync("tomatoes?", null);

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

        var result = await service.AskAsync("где мой заказ ORD-001?", null);

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

        var result = await service.AskAsync("сколько стоит доставка?", null);

        Assert.True(result.IsSuccess);
        _deliveryZoneRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_TwoConsecutiveToolCalls_ExecutesBothAndReturnsFinalText()
    {
        // Регрессия: с историей диалога модель иногда вызывает инструмент
        // повторно на втором круге вместо того, чтобы сразу вернуть текст —
        // раньше AskAsync обрабатывал только один раунд tool_calls и на
        // втором инструменте ошибочно считал, что текстового ответа нет.
        _currentUser.Setup(c => c.UserId).Returns(42);
        _customerProfileRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(new CustomerProfile { Id = 7, UserId = 42 });
        _orderRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Order { Id = 1, OrderNumber = "ORD-001", CustomerId = 7, Status = Domain.Enums.OrderStatus.InDelivery, TotalAmount = 150, DeliveryAddress = "ул. Рудаки 1" }
        ]);
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new DeliveryZone { Id = 1, Region = "Душанбе", District = "Сино", BasePrice = 20, PricePerKm = 2, IsActive = true }
        ]);

        var first = GroqToolCallResponse("call_1", "get_order_status", "{\"orderNumber\":\"ORD-001\"}");
        var second = GroqToolCallResponse("call_2", "get_delivery_info", "{}");
        var third = GroqTextResponse("{\"intent\":\"orders\",\"message\":\"Заказ ORD-001 в пути, доставка по Душанбе от 20 сомони\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second), (HttpStatusCode.OK, third));
        var service = CreateService(handler);

        var history = new List<AssistantHistoryMessageDto>
        {
            new() { Role = "user", Text = "статус заказа ORD-001?" },
            new() { Role = "assistant", Text = "Заказ ORD-001 в пути." }
        };

        var result = await service.AskAsync("а сколько будет стоить доставка туда же?", history);

        Assert.True(result.IsSuccess);
        Assert.Equal("orders", result.Data!.Intent);
        Assert.Equal("Заказ ORD-001 в пути, доставка по Душанбе от 20 сомони", result.Data.Message);
        _orderRepository.Verify(r => r.GetAllAsync(), Times.Once);
        _deliveryZoneRepository.Verify(r => r.GetAllAsync(), Times.Once);
        handler.Protected().Verify("SendAsync", Times.Exactly(3), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_ToolCallsExceedMaxRounds_StopsAfterThreeRoundsWithoutInfiniteLoop()
    {
        _productListingRepository.Setup(r => r.SearchAsync(It.IsAny<string>())).ReturnsAsync([]);

        var alwaysToolCall = GroqToolCallResponse("call_x", "search_products", "{\"query\":\"tomato\"}");
        var handler = MockHandlerSequence(
            (HttpStatusCode.OK, alwaysToolCall),
            (HttpStatusCode.OK, alwaysToolCall),
            (HttpStatusCode.OK, alwaysToolCall));
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?", null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
        handler.Protected().Verify("SendAsync", Times.Exactly(3), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    // === Полный доступ к данным своей роли (2026-08-02) ===

    [Fact]
    public async Task AskAsync_CustomerGetMyOrders_CallsSelfFilteredOrderService()
    {
        // IOrderService.GetAllAsync() уже сам self-фильтрует по currentUser
        // (проверено чтением исходника OrderService перед подключением) —
        // здесь достаточно убедиться, что AiAssistantService зовёт именно
        // его и корректно форматирует результат, без собственной доп.
        // фильтрации (которая была бы дублированием и точкой рассинхрона).
        _orderService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IEnumerable<GetOrderDto>>.Ok(
        [
            new GetOrderDto { Id = 1, OrderNumber = "ORD-001", CustomerId = 7, FarmerId = 1, Status = Domain.Enums.OrderStatus.InDelivery, DeliveryAddress = "ул. Рудаки 1", Region = "Душанбе", District = "Сино", TotalAmount = 150, CreatedAt = DateTime.UtcNow }
        ]));

        var first = GroqToolCallResponse("call_1", "get_my_orders", "{}");
        var second = GroqTextResponse("{\"intent\":\"orders\",\"message\":\"У вас один заказ ORD-001, он в пути\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("покажи все мои заказы", null);

        Assert.True(result.IsSuccess);
        _orderService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_CustomerGetMyFavorites_EnrichesWithProductTitleAndPrice()
    {
        _favoriteService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IEnumerable<GetFavoriteDto>>.Ok(
        [
            new GetFavoriteDto { Id = 1, CustomerId = 42, ProductListingId = 5, CreatedAt = DateTime.UtcNow }
        ]));
        _productListingRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new ProductListing { Id = 5, Title = "Морковь", RetailPricePerKg = 8 });

        var first = GroqToolCallResponse("call_1", "get_my_favorites", "{}");
        var second = GroqTextResponse("{\"intent\":\"none\",\"message\":\"В избранном у вас морковь\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("что у меня в избранном?", null);

        Assert.True(result.IsSuccess);
        _favoriteService.Verify(s => s.GetAllAsync(), Times.Once);
        _productListingRepository.Verify(r => r.GetByIdAsync(5), Times.Once);
    }

    [Fact]
    public async Task AskAsync_CustomerGetMyReviews_FiltersOutOtherCustomersReviews()
    {
        // ReviewService.GetAllAsync() намеренно публичный (витрина отзывов
        // фермера) — ownership-фильтрация "мои отзывы" целиком реализована
        // внутри AiAssistantService, поэтому это единственное место, где её
        // действительно нужно проверить юнит-тестом на утечку чужих данных.
        _currentUser.Setup(c => c.UserId).Returns(42);
        _customerProfileRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(new CustomerProfile { Id = 7, UserId = 42 });
        _reviewService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IEnumerable<GetReviewDto>>.Ok(
        [
            new GetReviewDto { Id = 1, CustomerId = 7, FarmerId = 1, Rating = 5, Comment = "Отлично", CreatedAt = DateTime.UtcNow },
            new GetReviewDto { Id = 2, CustomerId = 99, FarmerId = 1, Rating = 1, Comment = "Чужой отзыв, не мой", CreatedAt = DateTime.UtcNow }
        ]));

        JsonNode? capturedToolResult = null;
        var handler = new Mock<HttpMessageHandler>();
        var responses = new Queue<string>(new[]
        {
            GroqToolCallResponse("call_1", "get_my_reviews", "{}"),
            GroqTextResponse("{\"intent\":\"none\",\"message\":\"У вас один отзыв\"}")
        });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                var text = await req.Content!.ReadAsStringAsync();
                var body = JsonNode.Parse(text)!.AsObject();
                var messages = body["messages"]!.AsArray();
                var toolMessage = messages.FirstOrDefault(m => m!["role"]!.GetValue<string>() == "tool");
                if (toolMessage is not null) capturedToolResult = JsonNode.Parse(toolMessage["content"]!.GetValue<string>());
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responses.Dequeue()) };
            });
        var service = CreateService(handler);

        var result = await service.AskAsync("какие отзывы я оставлял?", null);

        Assert.True(result.IsSuccess);
        // Если бы чужой отзыв (Id=2, CustomerId=99) просочился, в массиве
        // было бы 2 элемента вместо одного, и Id=2 присутствовал бы.
        var reviewArray = capturedToolResult!.AsArray();
        Assert.Single(reviewArray);
        Assert.Equal(1, reviewArray[0]!["Id"]!.GetValue<int>());
    }

    [Fact]
    public async Task AskAsync_CustomerGetMyProfile_ReturnsOwnProfileData()
    {
        _currentUser.Setup(c => c.UserId).Returns(42);
        _customerProfileRepository.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync(
            new CustomerProfile { Id = 7, UserId = 42, Region = "Душанбе", District = "Сино", DefaultAddress = "ул. Рудаки 1", CustomerType = CustomerType.Retail });

        var first = GroqToolCallResponse("call_1", "get_my_profile", "{}");
        var second = GroqTextResponse("{\"intent\":\"none\",\"message\":\"Ваш адрес: ул. Рудаки 1\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("мой профиль", null);

        Assert.True(result.IsSuccess);
        _customerProfileRepository.Verify(r => r.GetByUserIdAsync(42), Times.Once);
    }

    [Fact]
    public async Task AskAsync_FarmerGetMyOrders_CallsSelfFilteredOrderService()
    {
        _currentUser.Setup(c => c.Role).Returns("Farmer");
        _orderService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IEnumerable<GetOrderDto>>.Ok(
        [
            new GetOrderDto { Id = 1, OrderNumber = "ORD-500", CustomerId = 3, FarmerId = 1, Status = Domain.Enums.OrderStatus.Pending, DeliveryAddress = "ул. Ленина 1", Region = "Хатлон", District = "Бохтар", TotalAmount = 60, CreatedAt = DateTime.UtcNow }
        ]));

        var first = GroqToolCallResponse("call_1", "get_my_orders", "{}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"У вас один новый заказ ORD-500\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("какие у меня заказы?", null);

        Assert.True(result.IsSuccess);
        _orderService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_FarmerGetMyDocuments_CallsSelfFilteredFarmerDocumentService()
    {
        _currentUser.Setup(c => c.Role).Returns("Farmer");
        _farmerDocumentService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IEnumerable<GetFarmerDocumentDto>>.Ok(
        [
            new GetFarmerDocumentDto { Id = 1, FarmerProfileId = 4, DocumentType = FarmerDocumentType.Passport, FileUrl = "url", Status = DocumentReviewStatus.Approved, UploadedAt = DateTime.UtcNow }
        ]));

        var first = GroqToolCallResponse("call_1", "get_my_documents", "{}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"Ваш паспорт одобрен\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("мои документы", null);

        Assert.True(result.IsSuccess);
        _farmerDocumentService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_FarmerGetVerificationStatus_ResolvesOwnProfileByCurrentUserId()
    {
        _currentUser.Setup(c => c.Role).Returns("Farmer");
        _currentUser.Setup(c => c.UserId).Returns(15);
        _farmerProfileRepository.Setup(r => r.GetByUserIdAsync(15)).ReturnsAsync(
            new FarmerProfile { Id = 4, UserId = 15, FarmName = "Ферма Солнце", VerificationStatus = FarmerVerificationStatus.Verified, VerifiedAt = DateTime.UtcNow });

        var first = GroqToolCallResponse("call_1", "get_verification_status", "{}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"Ваш профиль подтверждён\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("я верифицирован?", null);

        Assert.True(result.IsSuccess);
        // Ключевая проверка владения: резолвим профиль по ID ИМЕННО текущего
        // пользователя (15), а не по какому-то другому — нет способа узнать
        // чужой статус верификации через этот tool.
        _farmerProfileRepository.Verify(r => r.GetByUserIdAsync(15), Times.Once);
    }

    [Fact]
    public async Task AskAsync_FarmerGetMyStaff_CallsSelfFilteredFarmerStaffMemberService()
    {
        _currentUser.Setup(c => c.Role).Returns("Farmer");
        _farmerStaffMemberService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IEnumerable<GetFarmerStaffMemberDto>>.Ok(
        [
            new GetFarmerStaffMemberDto { Id = 1, FarmerProfileId = 4, UserId = 20, Permissions = StaffPermissions.ManageProducts, IsActive = true }
        ]));

        var first = GroqToolCallResponse("call_1", "get_my_staff", "{}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"У вас один сотрудник\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("мои сотрудники", null);

        Assert.True(result.IsSuccess);
        _farmerStaffMemberService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_AdminGetAllProducts_ReturnsCatalogAcrossAllFarmers()
    {
        _currentUser.Setup(c => c.Role).Returns("Admin");
        _productListingService.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(Result<PagedResult<GetProductListingDto>>.Ok(
            PagedResult<GetProductListingDto>.Ok(
            [
                new GetProductListingDto { Id = 1, FarmerProfileId = 4, Title = "Морковь", Status = ListingStatus.Active, RetailPricePerKg = 8, Region = "Хатлон" },
                new GetProductListingDto { Id = 2, FarmerProfileId = 9, Title = "Картофель", Status = ListingStatus.Active, RetailPricePerKg = 5, Region = "Согд" }
            ], 2, 1, 20)));

        var first = GroqToolCallResponse("call_1", "get_all_products", "{}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"В каталоге 2 товара\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("покажи все товары", null);

        Assert.True(result.IsSuccess);
        _productListingService.Verify(s => s.GetAllAsync(1, 20), Times.Once);
    }

    [Fact]
    public async Task AskAsync_AdminGetAllOrders_CallsPagedServiceAcrossPlatform()
    {
        _currentUser.Setup(c => c.Role).Returns("Admin");
        _orderService.Setup(s => s.GetPagedAsync(It.IsAny<PagedRequest>(), null)).ReturnsAsync(Result<PagedResult<GetOrderDto>>.Ok(
            PagedResult<GetOrderDto>.Ok(
            [
                new GetOrderDto { Id = 1, OrderNumber = "ORD-1", CustomerId = 1, FarmerId = 1, Status = Domain.Enums.OrderStatus.Pending, DeliveryAddress = "a", Region = "r", District = "d", TotalAmount = 10, CreatedAt = DateTime.UtcNow },
                new GetOrderDto { Id = 2, OrderNumber = "ORD-2", CustomerId = 2, FarmerId = 2, Status = Domain.Enums.OrderStatus.Completed, DeliveryAddress = "b", Region = "r", District = "d", TotalAmount = 20, CreatedAt = DateTime.UtcNow }
            ], 2, 1, 20)));

        var first = GroqToolCallResponse("call_1", "get_all_orders", "{}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"На платформе 2 заказа\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("все заказы", null);

        Assert.True(result.IsSuccess);
        _orderService.Verify(s => s.GetPagedAsync(It.IsAny<PagedRequest>(), null), Times.Once);
    }

    [Fact]
    public async Task AskAsync_AdminGetUsersList_FiltersByRoleWhenSpecified()
    {
        _currentUser.Setup(c => c.Role).Returns("Admin");
        _userService.Setup(s => s.GetPagedAsync(It.IsAny<PagedRequest>(), UserRole.Farmer, null)).ReturnsAsync(Result<PagedResult<GetUserDto>>.Ok(
            PagedResult<GetUserDto>.Ok(
            [
                new GetUserDto { Id = 4, FullName = "Фермер Иван", Email = "farmer@market.tj", PhoneNumber = "123", Role = UserRole.Farmer, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            ], 1, 1, 20)));

        var first = GroqToolCallResponse("call_1", "get_users_list", "{\"role\":\"Farmer\"}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"Найден один фермер\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("покажи всех фермеров", null);

        Assert.True(result.IsSuccess);
        _userService.Verify(s => s.GetPagedAsync(It.IsAny<PagedRequest>(), UserRole.Farmer, null), Times.Once);
    }

    [Fact]
    public async Task AskAsync_AdminGetCouriers_ReturnsCourierList()
    {
        _currentUser.Setup(c => c.Role).Returns("Admin");
        _courierProfileService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IEnumerable<GetCourierProfileDto>>.Ok(
        [
            new GetCourierProfileDto { Id = 1, UserId = 30, TransportType = "Car", VehicleNumber = "01A123", Region = "Душанбе", District = "Сино", IsAvailable = true, IsActive = true }
        ]));

        var first = GroqToolCallResponse("call_1", "get_couriers", "{}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"У нас один курьер\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("список курьеров", null);

        Assert.True(result.IsSuccess);
        _courierProfileService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_AdminGetCommissions_ReturnsCommissionList()
    {
        _currentUser.Setup(c => c.Role).Returns("Admin");
        _commissionService.Setup(s => s.GetAllAsync()).ReturnsAsync(Result<IEnumerable<GetCommissionDto>>.Ok(
        [
            new GetCommissionDto { Id = 1, CategoryId = null, Percentage = 5, EffectiveFrom = DateTime.UtcNow }
        ]));

        var first = GroqToolCallResponse("call_1", "get_commissions", "{}");
        var second = GroqTextResponse("{\"intent\":\"info\",\"message\":\"Комиссия 5%\"}");
        var handler = MockHandlerSequence((HttpStatusCode.OK, first), (HttpStatusCode.OK, second));
        var service = CreateService(handler);

        var result = await service.AskAsync("какая у нас комиссия?", null);

        Assert.True(result.IsSuccess);
        _commissionService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AskAsync_AdminGetDeliveryZones_IncludesInactiveZonesUnlikeCustomerTool()
    {
        // Отличие от customer-инструмента get_delivery_info (тот показывает
        // только активные зоны) — админу нужно видеть ВСЕ зоны, включая
        // неактивные, чтобы ими управлять.
        _currentUser.Setup(c => c.Role).Returns("Admin");
        _deliveryZoneRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new DeliveryZone { Id = 1, Region = "Душанбе", District = "Сино", BasePrice = 20, PricePerKm = 2, IsActive = true },
            new DeliveryZone { Id = 2, Region = "Хатлон", District = "Бохтар", BasePrice = 15, PricePerKm = 1.5m, IsActive = false }
        ]);

        JsonNode? capturedToolResult = null;
        var handler = new Mock<HttpMessageHandler>();
        var responses = new Queue<string>(new[]
        {
            GroqToolCallResponse("call_1", "get_delivery_zones", "{}"),
            GroqTextResponse("{\"intent\":\"info\",\"message\":\"Всего 2 зоны\"}")
        });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                var text = await req.Content!.ReadAsStringAsync();
                var body = JsonNode.Parse(text)!.AsObject();
                var messages = body["messages"]!.AsArray();
                var toolMessage = messages.FirstOrDefault(m => m!["role"]!.GetValue<string>() == "tool");
                if (toolMessage is not null) capturedToolResult = JsonNode.Parse(toolMessage["content"]!.GetValue<string>());
                return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responses.Dequeue()) };
            });
        var service = CreateService(handler);

        var result = await service.AskAsync("покажи зоны доставки", null);

        Assert.True(result.IsSuccess);
        var zoneArray = capturedToolResult!.AsArray();
        Assert.Equal(2, zoneArray.Count);
        Assert.Contains(zoneArray, z => z!["IsActive"]!.GetValue<bool>() == false);
    }

    [Fact]
    public async Task AskAsync_FarmerProposeUpdateListing_ReturnsActionPendingWithoutSecondHttpCall()
    {
        _currentUser.Setup(c => c.Role).Returns("Farmer");
        _productListingService.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(Result<GetProductListingDto?>.Ok(new GetProductListingDto { Id = 5, Title = "Картофель" }));

        var body = GroqToolCallResponse("call_2", "propose_update_listing", "{\"listingId\":5,\"field\":\"price\",\"value\":\"20\"}");
        var handler = MockHandler(HttpStatusCode.OK, body);
        var service = CreateService(handler);

        var result = await service.AskAsync("подними цену на картофель до 20", null);

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

        var result = await service.AskAsync("отклони жалобу 9", null);

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

        var result = await service.AskAsync("tomatoes?", null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.InternalServerError, result.ErrorType);
    }

    [Fact]
    public async Task AskAsync_NoTextAndNoToolCall_ReturnsFailure()
    {
        var handler = MockHandler(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":null}}]}""");
        var service = CreateService(handler);

        var result = await service.AskAsync("tomatoes?", null);

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

        var result = await service.AskAsync("tomatoes?", null);

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

        var result = await service.AskAsync("tomatoes?", null);

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

        var result = await service.AskAsync("tomatoes?", null);

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

        var result = await service.AskAsync("tomatoes?", null);

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

        var result = await service.AskAsync("tomatoes?", null);

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
