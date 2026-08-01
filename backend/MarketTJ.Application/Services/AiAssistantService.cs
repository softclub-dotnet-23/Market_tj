using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MarketTJ.Application.Common;
using MarketTJ.Application.Dto.AiAssistantDto;
using MarketTJ.Application.Dto.ProductListingDto;
using MarketTJ.Application.Interfaces.Repositories;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.Application.Results;
using MarketTJ.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketTJ.Application.Services;

// AI-ассистент (Groq API, OpenAI-совместимый формат) — осознанное отклонение
// от раздела 3 ТЗ («В MVP не входят: искусственный интеллект»), подтверждено
// пользователем явно, зафиксировано в TZ_MarketTJ_ClaudeCode.md, раздел 38.
// Изначально — только покупатель/гость (поиск по каталогу). С 2026-08-01 —
// роль-осознанный: покупателю доступен поиск по каталогу, фермеру и админу —
// информационные вопросы по своим данным плюс ПРЕДЛОЖЕНИЯ действий (см.
// AssistantActionDto — сам ассистент ничего не мутирует, только предлагает,
// реальное выполнение — через ExecuteActionAsync после подтверждения
// пользователем на фронтенде, с повторной проверкой прав на сервере).
// Провайдер сменён с Google Gemini на Groq 2026-08-01 — Gemini на бесплатном
// тарифе в текущем регионе требует привязанный billing даже для free tier
// (quota=0 без него), у Groq есть настоящий free tier без карты.
public class AiAssistantService(
    HttpClient httpClient,
    IProductListingRepository productListingRepository,
    IProductListingService productListingService,
    IFarmerProfileRepository farmerProfileRepository,
    IFarmerProfileService farmerProfileService,
    IReportedListingService reportedListingService,
    IAnalyticsService analyticsService,
    IOrderRepository orderRepository,
    ICustomerProfileRepository customerProfileRepository,
    IDeliveryZoneRepository deliveryZoneRepository,
    ICurrentUserService currentUser,
    IConfiguration configuration,
    ILogger<AiAssistantService> logger) : IAiAssistantService
{
    // Актуальная бесплатная модель Groq с поддержкой tool calling на
    // 2026-08-01 (console.groq.com/docs/models) — Meta Llama 3.3 70B.
    private const string Model = "llama-3.3-70b-versatile";
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    // Общие для всех трёх ролей требования к тону ответа (2026-08-01, по явному
    // запросу пользователя) — без этого модель иногда отвечала однословно
    // ("помидоры") и не на языке вопроса. Placeholder {0} — специфика роли.
    private const string ResponseStyleInstruction =
        "ЯЗЫК ОТВЕТА: всегда отвечай на том же языке, на котором задан вопрос пользователя " +
        "(русский/таджикский/английский или другой) — определяй язык по тексту самого вопроса, " +
        "а не по каким-либо настройкам аккаунта. Поле message должно быть полностью на этом " +
        "языке. ПОЛНОТА ОТВЕТА: message — это полное развёрнутое предложение (или несколько), " +
        "которое реально отвечает на вопрос. Никогда не отвечай одним словом или обрывком фразы " +
        "(плохо: \"помидоры\"; хорошо: \"Да, у нас есть свежие помидоры от нескольких фермеров — " +
        "вот что нашлось: ...\"). Если вызывал инструмент — перескажи полученные данные своими " +
        "словами понятно и по-человечески, а не просто перечисли сырые цифры.";

    // Добавлено 2026-08-02 по явному запросу пользователя — ассистент иногда
    // отвечал не по смыслу вопроса, если тот был сформулирован нестандартно.
    private const string IntentUnderstandingInstruction =
        "ПОНИМАНИЕ ВОПРОСА: прежде чем отвечать или вызывать инструмент, определи истинную " +
        "цель (намерение) вопроса пользователя по смыслу, а не по буквальному совпадению слов. " +
        "Один и тот же запрос пользователь может сформулировать совершенно по-разному: другой " +
        "порядок слов, сокращения, опечатки, разговорный стиль, неполная фраза. Ориентируйся на " +
        "смысл, а не на точный текст. НЕДОСТАЮЩИЕ ДАННЫЕ: если для ответа не хватает конкретной " +
        "детали (например, спрашивают статус заказа, но не назвали номер) — НЕ угадывай и НЕ " +
        "вызывай инструмент с придуманным значением. Вместо этого верни ответ с intent, " +
        "подходящим для обычного информационного сообщения (для покупателя — \"none\", для " +
        "фермера/админа — \"info\"), и вежливо попроси именно эту недостающую деталь на языке " +
        "вопроса.";

    // Разные формулировки одного и того же намерения "статус заказа" — по
    // явному запросу пользователя (2026-08-02), т.к. это самый частый случай,
    // где ассистент отвечал невпопад на нестандартную фразировку.
    private const string CustomerFewShotExamples =
        "ПРИМЕРЫ (разные формулировки одного и того же намерения — во всех случаях реакция " +
        "должна быть одинаковой):\n" +
        "- \"где заказ ORD-123\", \"статус заказа ORD-123\", \"когда придёт мой заказ ORD-123\", " +
        "\"ORD-123 где\", \"track ORD-123\", \"фармоиши ORD-123 дар кучост\" — во всех случаях " +
        "вызови get_order_status(orderNumber=\"ORD-123\").\n" +
        "- \"статус заказа\" (без номера), \"где мой заказ\" (без номера) — номера нет, НЕ " +
        "вызывай инструмент, спроси номер заказа.\n" +
        "- \"есть помидоры\", \"нужны помидоры\", \"ищу помидоры\", \"продаёте ли вы помидоры\", " +
        "\"do you have tomatoes\" — во всех случаях вызови search_products(query=\"помидоры\"/" +
        "\"tomatoes\").\n\n";

    private const string CustomerSystemPrompt =
        "Ты AI-ассистент маркетплейса Market.tj — платформы, где фермеры продают свежую " +
        "продукцию напрямую покупателям. Общайся дружелюбно и по делу.\n\n" +
        ResponseStyleInstruction + "\n\n" +
        IntentUnderstandingInstruction + "\n\n" +
        CustomerFewShotExamples +
        "Инструменты (вызывай, когда вопрос требует конкретных данных):\n" +
        "- search_products(query) — ищет товары в каталоге по ключевому слову.\n" +
        "- get_order_status(orderNumber) — статус конкретного заказа текущего покупателя по " +
        "номеру заказа (доступно только авторизованным покупателям).\n" +
        "- get_delivery_info() — список зон доставки с базовой ценой и ценой за километр.\n\n" +
        "Без инструментов, своими словами, ты также должен уметь объяснять:\n" +
        "- Как оформить заказ: добавить нужные товары в корзину на странице товара или каталога, " +
        "перейти в оформление заказа (Checkout), указать адрес доставки и подтвердить.\n" +
        "- Где посмотреть свои заказы: в личном кабинете покупателя, раздел «Мои заказы» — " +
        "используй intent=\"orders\", чтобы показать кнопку перехода туда.\n" +
        "- Как работает доставка: курьер забирает товар у фермера и привозит по адресу " +
        "покупателя; стоимость зависит от региона/района — если пользователь спрашивает про " +
        "конкретную стоимость или зоны, вызови get_delivery_info.\n" +
        "- Как стать фермером: зарегистрироваться на платформе с ролью «Фермер» на странице " +
        "регистрации, заполнить профиль хозяйства (название, регион, район, адрес), после чего " +
        "аккаунт проходит проверку администратором — до подтверждения объявления публиковать " +
        "нельзя.\n\n" +
        "Верни СТРОГО JSON без markdown: {\"intent\":\"product|category|cart|orders|none\"," +
        "\"productId\":null,\"categoryId\":null,\"message\":\"\"}. product — когда речь про " +
        "один явный товар (после search_products, если нашёлся единственный явный кандидат — " +
        "заполни productId). category — несколько товаров одной категории. cart — если просит " +
        "перейти в корзину/оформить заказ. orders — если просит показать свои заказы или " +
        "спрашивает про статус заказа. none — для всех остальных вопросов (доставка, " +
        "регистрация фермера, общие вопросы о платформе) — message должен содержать полный " +
        "ответ.";

    private const string FarmerFewShotExamples =
        "ПРИМЕРЫ (разные формулировки одного и того же намерения — во всех случаях реакция " +
        "должна быть одинаковой):\n" +
        "- \"покажи мои товары\", \"какие у меня объявления\", \"мои листинги\", \"что я " +
        "продаю\" — во всех случаях вызови get_my_listings.\n" +
        "- \"как дела с продажами\", \"сколько я заработал\", \"сводка\", \"дашборд\" — во всех " +
        "случаях вызови get_dashboard.\n" +
        "- \"подними цену на картошку\" без указания, на сколько и на какое именно объявление — " +
        "не вызывай propose_update_listing с придуманными данными, сначала уточни, на какое " +
        "объявление и до какой цены.\n\n";

    private const string FarmerSystemPrompt =
        "Ты AI-ассистент маркетплейса Market.tj для ФЕРМЕРА (продавца), уже авторизованного " +
        "в системе. Общайся дружелюбно и по делу.\n\n" +
        ResponseStyleInstruction + "\n\n" +
        IntentUnderstandingInstruction + "\n\n" +
        FarmerFewShotExamples +
        "Инструменты: get_dashboard — сводка по моим товарам/заказам/выручке; " +
        "get_my_listings — список МОИХ объявлений (можно фильтровать по статусу); " +
        "propose_update_listing — предложить изменить цену или статус ОДНОГО из МОИХ " +
        "объявлений (сам ничего не меняет — только предлагает фермеру подтвердить, " +
        "используй его как только фермер просит что-то изменить). Всегда вызывай " +
        "подходящий инструмент, если вопрос требует данных.\n\n" +
        "Верни СТРОГО JSON без markdown: {\"intent\":\"info\",\"message\":\"<полный развёрнутый " +
        "ответ на языке пользователя по полученным данным>\"}. Если инструмент не нужен — " +
        "тоже верни {\"intent\":\"info\",\"message\":\"...\"}.";

    private const string AdminFewShotExamples =
        "ПРИМЕРЫ (разные формулировки одного и того же намерения — во всех случаях реакция " +
        "должна быть одинаковой):\n" +
        "- \"жалобы\", \"есть жалобы?\", \"покажи жалобы на модерацию\", \"что на рассмотрении\" " +
        "— во всех случаях вызови get_pending_reports.\n" +
        "- \"кто ждёт проверки\", \"новые фермеры\", \"верификации\" — во всех случаях вызови " +
        "get_pending_verifications.\n" +
        "- \"отклони жалобу\" без номера жалобы — не вызывай propose_resolve_report с " +
        "придуманным reportId, сначала уточни, какую именно жалобу.\n\n";

    private const string AdminSystemPrompt =
        "Ты AI-ассистент маркетплейса Market.tj для АДМИНИСТРАТОРА, уже авторизованного " +
        "в системе. Общайся дружелюбно и по делу.\n\n" +
        ResponseStyleInstruction + "\n\n" +
        IntentUnderstandingInstruction + "\n\n" +
        AdminFewShotExamples +
        "Инструменты: get_dashboard — сводка по всей платформе (заказы, выручка, " +
        "пользователи); get_pending_verifications — фермеры, ожидающие проверки; " +
        "get_pending_reports — жалобы на объявления, ожидающие рассмотрения; " +
        "propose_resolve_report — предложить рассмотреть жалобу (Reviewed) или отклонить " +
        "(Dismissed) — сам ничего не меняет, только предлагает админу подтвердить. Всегда " +
        "вызывай подходящий инструмент, если вопрос требует данных.\n\n" +
        "Верни СТРОГО JSON без markdown: {\"intent\":\"info\",\"message\":\"<полный развёрнутый " +
        "ответ на языке пользователя по полученным данным>\"}. Если инструмент не нужен — " +
        "тоже верни {\"intent\":\"info\",\"message\":\"...\"}.";

    // Сколько последних реплик истории учитывать (не считая текущего вопроса) —
    // ограничение и на размер запроса к Groq, и на то, чтобы старый контекст
    // не перевешивал сам текущий вопрос. 10 реплик = 5 пар вопрос/ответ.
    private const int MaxHistoryMessages = 10;

    public async Task<Result<AssistantResponseDto>> AskAsync(string message, List<AssistantHistoryMessageDto>? history)
    {
        try
        {
            var apiKey = configuration["Groq:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogError("Groq:ApiKey не задан (appsettings.json / User Secrets)");
                return Result<AssistantResponseDto>.Fail("AI-ассистент временно недоступен", ErrorType.InternalServerError);
            }

            var role = currentUser.Role;
            var (systemPrompt, tools) = BuildPromptAndTools(role);

            var messages = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = systemPrompt }
            };

            // История добавлена 2026-08-02 — раньше каждый запрос был изолирован,
            // и ассистент "не помнил" предыдущий вопрос в этом же диалоге (см.
            // AiAssistantWidget.tsx — фронтенд теперь передаёт её сюда).
            if (history is not null)
            {
                foreach (var h in history.TakeLast(MaxHistoryMessages))
                {
                    var historyRole = h.Role == "assistant" ? "assistant" : "user";
                    messages.Add(new JsonObject { ["role"] = historyRole, ["content"] = h.Text });
                }
            }

            messages.Add(new JsonObject { ["role"] = "user", ["content"] = message });

            var response = await SendToGroqAsync(apiKey, tools, messages);
            var responseMessage = GetFirstChoiceMessage(response);

            var toolCall = responseMessage?["tool_calls"]?.AsArray().FirstOrDefault();

            if (toolCall is not null)
            {
                var function = toolCall["function"]!;
                var functionName = function["name"]!.GetValue<string>();
                var argumentsJson = function["arguments"]?.GetValue<string>();
                var args = string.IsNullOrWhiteSpace(argumentsJson) ? null : JsonNode.Parse(argumentsJson);
                var toolCallId = toolCall["id"]!.GetValue<string>();

                // propose_* — предложение действия формируется сразу, без второго
                // обращения к Groq: модель просто должна была вызвать инструмент
                // с правильными параметрами, сочинять текст ей тут не нужно.
                if (functionName == "propose_update_listing")
                {
                    return await BuildProposeUpdateListingResponseAsync(args);
                }
                if (functionName == "propose_resolve_report")
                {
                    return await BuildProposeResolveReportResponseAsync(args);
                }

                var toolResultText = await ExecuteReadToolAsync(functionName, args);

                messages.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = null,
                    ["tool_calls"] = new JsonArray { toolCall.DeepClone() }
                });
                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = toolCallId,
                    ["content"] = toolResultText
                });

                response = await SendToGroqAsync(apiKey, tools, messages);
                responseMessage = GetFirstChoiceMessage(response);
            }

            var textContent = responseMessage?["content"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(textContent))
            {
                logger.LogError("Groq не вернул текстовый ответ: {Response}", response.ToJsonString());
                return Result<AssistantResponseDto>.Fail("Не удалось получить ответ ассистента", ErrorType.InternalServerError);
            }

            var json = textContent.Trim().Trim('`');
            if (json.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                json = json[4..].Trim();
            }

            var parsed = JsonSerializer.Deserialize<AssistantResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed is null)
            {
                logger.LogError("Не удалось распарсить JSON от ассистента: {Json}", json);
                return Result<AssistantResponseDto>.Fail("Не удалось разобрать ответ ассистента", ErrorType.InternalServerError);
            }

            return Result<AssistantResponseDto>.Ok(parsed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обращении к AI-ассистенту");
            return Result<AssistantResponseDto>.Fail("Ошибка AI-ассистента", ErrorType.InternalServerError);
        }
    }

    public async Task<Result<string>> ExecuteActionAsync(ExecuteAssistantActionDto dto)
    {
        try
        {
            return dto.Type switch
            {
                "update_listing" => await ExecuteUpdateListingAsync(dto.Params),
                "resolve_report" => await ExecuteResolveReportAsync(dto.Params),
                _ => Result<string>.Fail("Неизвестное действие", ErrorType.BadRequest)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при выполнении действия ассистента {Type}", dto.Type);
            return Result<string>.Fail("Не удалось выполнить действие", ErrorType.InternalServerError);
        }
    }

    // === Построение промпта/инструментов по роли ===

    private (string SystemPrompt, JsonArray Tools) BuildPromptAndTools(string? role)
    {
        var customerTool = BuildFunctionDeclaration(
            "search_products", "Ищет товары в каталоге Market.tj по ключевому слову",
            ("query", "string", null, true));

        if (role == "Farmer")
        {
            var tools = new JsonArray
            {
                BuildFunctionDeclaration("get_dashboard", "Сводка по моим товарам, заказам и выручке"),
                BuildFunctionDeclaration("get_my_listings", "Список моих объявлений, можно отфильтровать по статусу",
                    ("status", "string", new[] { "Draft", "Active", "OutOfStock", "Archived" }, false)),
                BuildFunctionDeclaration("propose_update_listing", "Предложить изменить цену или статус одного из моих объявлений",
                    ("listingId", "integer", null, true),
                    ("field", "string", new[] { "price", "status" }, true),
                    ("value", "string", null, true)),
            };
            return (FarmerSystemPrompt, tools);
        }

        if (role == "Admin")
        {
            var tools = new JsonArray
            {
                BuildFunctionDeclaration("get_dashboard", "Сводная аналитика по всей платформе"),
                BuildFunctionDeclaration("get_pending_verifications", "Список фермеров, ожидающих проверки"),
                BuildFunctionDeclaration("get_pending_reports", "Список жалоб на объявления, ожидающих рассмотрения"),
                BuildFunctionDeclaration("propose_resolve_report", "Предложить рассмотреть или отклонить жалобу на объявление",
                    ("reportId", "integer", null, true),
                    ("resolution", "string", new[] { "Reviewed", "Dismissed" }, true)),
            };
            return (AdminSystemPrompt, tools);
        }

        // Покупатель, курьер или гость (без токена) — тот же customer-flow, что и раньше,
        // плюс статус заказа и информация о доставке (2026-08-01).
        var customerTools = new JsonArray
        {
            customerTool,
            BuildFunctionDeclaration("get_order_status", "Статус конкретного заказа текущего покупателя по номеру заказа",
                ("orderNumber", "string", null, true)),
            BuildFunctionDeclaration("get_delivery_info", "Список зон доставки с базовой ценой и ценой за километр"),
        };
        return (CustomerSystemPrompt, customerTools);
    }

    // Формат Groq/OpenAI: {"type":"function","function":{name,description,parameters}} —
    // отличается от Gemini обёрткой type+function, сама схема parameters та же.
    private static JsonObject BuildFunctionDeclaration(
        string name, string description, params (string Name, string Type, string[]? Enum, bool Required)[] parameters)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (var p in parameters)
        {
            var schema = new JsonObject { ["type"] = p.Type };
            if (p.Enum is not null)
            {
                schema["enum"] = new JsonArray(p.Enum.Select(e => JsonValue.Create(e)).ToArray());
            }
            properties[p.Name] = schema;
            if (p.Required) required.Add(p.Name);
        }

        return new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = description,
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = properties,
                    ["required"] = required
                }
            }
        };
    }

    // === Инструменты только на чтение (идут во второй запрос к Gemini) ===

    private async Task<string> ExecuteReadToolAsync(string functionName, JsonNode? args)
        => functionName switch
        {
            "search_products" => await ExecuteSearchProductsAsync(args),
            "get_order_status" => await ExecuteGetOrderStatusAsync(args),
            "get_delivery_info" => await ExecuteGetDeliveryInfoAsync(),
            "get_dashboard" => await ExecuteGetDashboardAsync(),
            "get_my_listings" => await ExecuteGetMyListingsAsync(args),
            "get_pending_verifications" => await ExecuteGetPendingVerificationsAsync(),
            "get_pending_reports" => await ExecuteGetPendingReportsAsync(),
            _ => "Неизвестный инструмент"
        };

    private async Task<string> ExecuteSearchProductsAsync(JsonNode? args)
    {
        var query = args?["query"]?.GetValue<string>() ?? "";
        var found = await productListingRepository.SearchAsync(query);
        return found.Count == 0
            ? "Ничего не найдено"
            : JsonSerializer.Serialize(found.Select(p => new { p.Id, p.Title, p.RetailPricePerKg }));
    }

    private async Task<string> ExecuteGetOrderStatusAsync(JsonNode? args)
    {
        if (currentUser.UserId is null) return "Нет доступа";
        var profile = await customerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        if (profile is null) return "Профиль покупателя не найден";

        var orderNumber = args?["orderNumber"]?.GetValue<string>()?.Trim();
        if (string.IsNullOrWhiteSpace(orderNumber)) return "Не указан номер заказа";

        var all = await orderRepository.GetAllAsync();
        var order = all.FirstOrDefault(o => o.CustomerId == profile.Id && string.Equals(o.OrderNumber, orderNumber, StringComparison.OrdinalIgnoreCase));
        if (order is null) return "Заказ с таким номером не найден среди ваших заказов";

        return JsonSerializer.Serialize(new
        {
            order.OrderNumber,
            Status = order.Status.ToString(),
            order.TotalAmount,
            order.DeliveryAddress,
            order.CreatedAt
        });
    }

    private async Task<string> ExecuteGetDeliveryInfoAsync()
    {
        var zones = await deliveryZoneRepository.GetAllAsync();
        var active = zones.Where(z => z.IsActive)
            .Select(z => new { z.Region, z.District, z.BasePrice, z.PricePerKm })
            .ToList();
        return active.Count == 0 ? "Информация о зонах доставки пока не настроена" : JsonSerializer.Serialize(active);
    }

    private async Task<string> ExecuteGetDashboardAsync()
    {
        if (currentUser.IsAdmin())
        {
            var result = await analyticsService.GetAdminDashboardAsync();
            return result.IsSuccess ? JsonSerializer.Serialize(result.Data) : "Не удалось получить данные аналитики";
        }

        if (currentUser.UserId is null) return "Нет доступа";
        var farmerResult = await analyticsService.GetFarmerDashboardAsync(currentUser.UserId.Value);
        return farmerResult.IsSuccess ? JsonSerializer.Serialize(farmerResult.Data) : "Не удалось получить данные аналитики";
    }

    private async Task<string> ExecuteGetMyListingsAsync(JsonNode? args)
    {
        if (currentUser.UserId is null) return "Нет доступа";
        var profile = await farmerProfileRepository.GetByUserIdAsync(currentUser.UserId.Value);
        if (profile is null) return "Профиль фермера не найден";

        var statusFilter = args?["status"]?.GetValue<string>();
        var all = await productListingRepository.GetAllAsync();
        var mine = all.Where(l => l.FarmerProfileId == profile.Id);
        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<ListingStatus>(statusFilter, out var status))
        {
            mine = mine.Where(l => l.Status == status);
        }

        var list = mine.Select(l => new { l.Id, l.Title, Status = l.Status.ToString(), l.RetailPricePerKg, l.AvailableQuantity }).ToList();
        return list.Count == 0 ? "Объявлений с такими параметрами нет" : JsonSerializer.Serialize(list);
    }

    private async Task<string> ExecuteGetPendingVerificationsAsync()
    {
        var result = await farmerProfileService.GetAllAsync();
        if (!result.IsSuccess) return "Не удалось получить данные";

        var pending = result.Data!.Where(f => f.VerificationStatus == FarmerVerificationStatus.Pending)
            .Select(f => new { f.Id, f.FarmName, f.Region, f.CreatedAt })
            .ToList();
        return pending.Count == 0 ? "Нет фермеров, ожидающих проверки" : JsonSerializer.Serialize(pending);
    }

    private async Task<string> ExecuteGetPendingReportsAsync()
    {
        var result = await reportedListingService.GetPagedAsync(new PagedRequest { PageSize = 20 }, ReportStatus.Pending);
        if (!result.IsSuccess) return "Не удалось получить данные";

        var items = result.Data!.Items
            .Select(r => new { r.Id, r.ProductListingId, Reason = r.Reason.ToString(), r.Comment, r.CreatedAt })
            .ToList();
        return items.Count == 0 ? "Нет жалоб, ожидающих рассмотрения" : JsonSerializer.Serialize(items);
    }

    // === propose_* — формируют AssistantActionDto напрямую, без второго round-trip ===

    private async Task<Result<AssistantResponseDto>> BuildProposeUpdateListingResponseAsync(JsonNode? args)
    {
        var listingId = args?["listingId"]?.GetValue<int>() ?? 0;
        var field = args?["field"]?.GetValue<string>() ?? "";
        var value = args?["value"]?.GetValue<string>() ?? "";

        var existing = await productListingService.GetByIdAsync(listingId);
        if (!existing.IsSuccess || existing.Data is null)
        {
            return Result<AssistantResponseDto>.Ok(new AssistantResponseDto { Intent = "info", Message = "Объявление не найдено" });
        }

        var confirmLabel = field switch
        {
            "price" => $"Изменить цену «{existing.Data.Title}» на {value} с./кг?",
            "status" => $"Изменить статус «{existing.Data.Title}» на {value}?",
            _ => $"Изменить «{existing.Data.Title}»?"
        };

        return Result<AssistantResponseDto>.Ok(new AssistantResponseDto
        {
            Intent = "action_pending",
            Message = confirmLabel,
            Action = new AssistantActionDto
            {
                Type = "update_listing",
                Params = new Dictionary<string, string> { ["listingId"] = listingId.ToString(), ["field"] = field, ["value"] = value },
                ConfirmLabel = confirmLabel
            }
        });
    }

    private async Task<Result<AssistantResponseDto>> BuildProposeResolveReportResponseAsync(JsonNode? args)
    {
        var reportId = args?["reportId"]?.GetValue<int>() ?? 0;
        var resolution = args?["resolution"]?.GetValue<string>() ?? "";

        var report = await reportedListingService.GetByIdAsync(reportId);
        if (!report.IsSuccess || report.Data is null)
        {
            return Result<AssistantResponseDto>.Ok(new AssistantResponseDto { Intent = "info", Message = "Жалоба не найдена" });
        }

        var verb = resolution == "Dismissed" ? "отклонить" : "пометить рассмотренной";
        var confirmLabel = $"{char.ToUpper(verb[0])}{verb[1..]} жалобу на объявление #{report.Data.ProductListingId}?";

        return Result<AssistantResponseDto>.Ok(new AssistantResponseDto
        {
            Intent = "action_pending",
            Message = confirmLabel,
            Action = new AssistantActionDto
            {
                Type = "resolve_report",
                Params = new Dictionary<string, string> { ["reportId"] = reportId.ToString(), ["resolution"] = resolution },
                ConfirmLabel = confirmLabel
            }
        });
    }

    // === Реальное выполнение — только отсюда, после подтверждения на фронтенде ===

    private async Task<Result<string>> ExecuteUpdateListingAsync(Dictionary<string, string> p)
    {
        if (!p.TryGetValue("listingId", out var listingIdStr) || !int.TryParse(listingIdStr, out var listingId))
            return Result<string>.Fail("Некорректный listingId", ErrorType.Validation);
        if (!p.TryGetValue("field", out var field) || !p.TryGetValue("value", out var value))
            return Result<string>.Fail("Не переданы параметры действия", ErrorType.Validation);

        var existingResult = await productListingService.GetByIdAsync(listingId);
        if (!existingResult.IsSuccess || existingResult.Data is null)
            return Result<string>.Fail("Объявление не найдено", ErrorType.NotFound);

        var existing = existingResult.Data;
        var updateDto = new UpdateProductListingDto
        {
            Id = existing.Id,
            FarmerProfileId = existing.FarmerProfileId,
            CategoryId = existing.CategoryId,
            Unit = existing.Unit,
            Title = existing.Title,
            Description = existing.Description,
            RetailPricePerKg = existing.RetailPricePerKg,
            WholesalePricePerKg = existing.WholesalePricePerKg,
            WholesaleMinimumQuantity = existing.WholesaleMinimumQuantity,
            AvailableQuantity = existing.AvailableQuantity,
            MinimumOrderQuantity = existing.MinimumOrderQuantity,
            HarvestDate = existing.HarvestDate,
            ExpectedHarvestDate = existing.ExpectedHarvestDate,
            QualityGrade = existing.QualityGrade,
            Region = existing.Region,
            District = existing.District,
            Address = existing.Address,
            Status = existing.Status
        };

        switch (field)
        {
            case "price":
                if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var price) || price <= 0)
                    return Result<string>.Fail("Некорректная цена", ErrorType.Validation);
                updateDto.RetailPricePerKg = price;
                break;
            case "status":
                if (!Enum.TryParse<ListingStatus>(value, out var status))
                    return Result<string>.Fail("Некорректный статус", ErrorType.Validation);
                updateDto.Status = status;
                break;
            default:
                return Result<string>.Fail("Неизвестное поле для изменения", ErrorType.Validation);
        }

        // Владение объявлением проверяется внутри ProductListingService.UpdateAsync
        // (OwnsAsync читает currentUser, а не то, что предложил AI) — здесь
        // намеренно нет собственной проверки, чтобы не разойтись с уже
        // проверенной бизнес-логикой.
        return await productListingService.UpdateAsync(listingId, updateDto);
    }

    private async Task<Result<string>> ExecuteResolveReportAsync(Dictionary<string, string> p)
    {
        // ReportedListingService.ResolveAsync доверяет переданному adminId, сам
        // роль не проверяет (раньше этот метод не был подключён ни к одному
        // контроллеру) — проверка роли здесь обязательна.
        if (!currentUser.IsAdmin())
            return Result<string>.Fail("Доступно только администратору", ErrorType.Forbidden);
        if (currentUser.UserId is null)
            return Result<string>.Fail("Требуется авторизация", ErrorType.Unauthorized);

        if (!p.TryGetValue("reportId", out var reportIdStr) || !int.TryParse(reportIdStr, out var reportId))
            return Result<string>.Fail("Некорректный reportId", ErrorType.Validation);
        if (!p.TryGetValue("resolution", out var resolutionStr) || !Enum.TryParse<ReportStatus>(resolutionStr, out var resolution))
            return Result<string>.Fail("Некорректное решение", ErrorType.Validation);

        return await reportedListingService.ResolveAsync(reportId, resolution, currentUser.UserId.Value);
    }

    // === Groq HTTP (OpenAI-совместимый chat completions) ===

    private static JsonObject? GetFirstChoiceMessage(JsonObject response)
        => response["choices"]?.AsArray().FirstOrDefault()?["message"]?.AsObject();

    // llama-3.3-70b-versatile на Groq изредка формирует вызов инструмента текстом
    // (<function=name{...}>) вместо структурированного tool_calls — Groq в ответ
    // отдаёт 400 с code="tool_use_failed". Наблюдалось на живой проверке
    // 2026-08-01: не постоянная ошибка запроса, а плавающая особенность
    // генерации у самой модели. Три уровня восстановления, от дешёвого к
    // дорогому: 1) модель иногда успевает сгенерировать вслед за неудачным
    // вызовом и корректный финальный JSON-ответ — если он есть в
    // failed_generation, используем его без лишнего запроса; 2) иначе — один
    // повтор того же запроса (обычно проходит); 3) если и это не помогло —
    // финальная попытка вообще без инструментов (tool_choice="none"), чтобы
    // гарантированно получить хоть какой-то текстовый ответ, а не отдать
    // пользователю "AI-ассистент недоступен".
    private async Task<JsonObject> SendToGroqAsync(string apiKey, JsonArray tools, JsonArray messages)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var (body, statusCode, rawBody) = await PostToGroqAsync(apiKey, tools, messages, toolChoice: "auto");
            if (body is not null) return body;

            if (!IsToolUseFailed(rawBody))
            {
                logger.LogError("Groq API вернул {StatusCode}: {Body}", statusCode, rawBody);
                throw new InvalidOperationException($"Groq API error {statusCode}");
            }

            var salvaged = ExtractTrailingJsonAnswer(rawBody);
            if (salvaged is not null)
            {
                logger.LogWarning("Groq вернул tool_use_failed, использую ответ, найденный в failed_generation (попытка {Attempt})", attempt);
                return WrapAsAssistantTextResponse(salvaged);
            }

            logger.LogWarning("Groq вернул tool_use_failed без пригодного ответа (попытка {Attempt})", attempt);
        }

        logger.LogWarning("Groq дважды вернул tool_use_failed, финальная попытка без инструментов");
        var (finalBody, finalStatus, finalRaw) = await PostToGroqAsync(apiKey, tools: null, messages, toolChoice: null);
        if (finalBody is not null) return finalBody;

        logger.LogError("Groq API вернул {StatusCode}: {Body}", finalStatus, finalRaw);
        throw new InvalidOperationException($"Groq API error {finalStatus}");
    }

    private static bool IsToolUseFailed(string responseBody)
    {
        try
        {
            return JsonNode.Parse(responseBody)?["error"]?["code"]?.GetValue<string>() == "tool_use_failed";
        }
        catch
        {
            return false;
        }
    }

    // Ищет последний JSON-объект вида {"intent":...} внутри error.failed_generation —
    // модель иногда пишет туда и неудавшийся текстовый вызов функции, и
    // корректный финальный ответ следом за ним, одним куском текста.
    private static string? ExtractTrailingJsonAnswer(string responseBody)
    {
        try
        {
            var failedGeneration = JsonNode.Parse(responseBody)?["error"]?["failed_generation"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(failedGeneration)) return null;

            var start = failedGeneration.LastIndexOf("{\"intent\"", StringComparison.Ordinal);
            if (start < 0) return null;

            var depth = 0;
            for (var i = start; i < failedGeneration.Length; i++)
            {
                if (failedGeneration[i] == '{') depth++;
                else if (failedGeneration[i] == '}' && --depth == 0)
                {
                    var candidate = failedGeneration[start..(i + 1)];
                    // Подтверждаем, что это реально валидный AssistantResponseDto,
                    // а не просто похожий на JSON фрагмент.
                    var parsed = JsonSerializer.Deserialize<AssistantResponseDto>(candidate, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return string.IsNullOrWhiteSpace(parsed?.Message) ? null : candidate;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static JsonObject WrapAsAssistantTextResponse(string content) => new JsonObject
    {
        ["choices"] = new JsonArray
        {
            new JsonObject { ["message"] = new JsonObject { ["role"] = "assistant", ["content"] = content } }
        }
    };

    private async Task<(JsonObject? Body, System.Net.HttpStatusCode StatusCode, string RawBody)> PostToGroqAsync(string apiKey, JsonArray? tools, JsonArray messages, string? toolChoice)
    {
        var requestBody = new JsonObject
        {
            ["model"] = Model,
            ["messages"] = messages.DeepClone(),
            // Ниже дефолта (1.0) — предсказуемые, точные ответы важнее
            // "творческих" формулировок для справочного ассистента маркетплейса
            // (2026-08-02, по явному запросу пользователя).
            ["temperature"] = 0.3
        };
        if (tools is not null)
        {
            requestBody["tools"] = tools.DeepClone();
        }
        if (toolChoice is not null)
        {
            requestBody["tool_choice"] = toolChoice;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (null, response.StatusCode, responseBody);
        }

        return (JsonNode.Parse(responseBody)!.AsObject(), response.StatusCode, responseBody);
    }
}
