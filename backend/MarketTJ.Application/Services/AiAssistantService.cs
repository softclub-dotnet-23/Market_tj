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

// AI-ассистент (Google Gemini API) — осознанное отклонение от раздела 3 ТЗ
// («В MVP не входят: искусственный интеллект»), подтверждено пользователем
// явно, зафиксировано в TZ_MarketTJ_ClaudeCode.md, раздел 38. Изначально —
// только покупатель/гость (поиск по каталогу). С 2026-08-01 — роль-осознанный:
// покупателю доступен поиск по каталогу, фермеру и админу — информационные
// вопросы по своим данным плюс ПРЕДЛОЖЕНИЯ действий (см. AssistantActionDto —
// сам ассистент ничего не мутирует, только предлагает, реальное выполнение —
// через ExecuteActionAsync после подтверждения пользователем на фронтенде,
// с повторной проверкой прав на сервере).
public class AiAssistantService(
    HttpClient httpClient,
    IProductListingRepository productListingRepository,
    IProductListingService productListingService,
    IFarmerProfileRepository farmerProfileRepository,
    IFarmerProfileService farmerProfileService,
    IReportedListingService reportedListingService,
    IAnalyticsService analyticsService,
    ICurrentUserService currentUser,
    IConfiguration configuration,
    ILogger<AiAssistantService> logger) : IAiAssistantService
{
    private const string Model = "gemini-2.0-flash";
    private const string ApiUrlTemplate = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";

    private const string CustomerSystemPrompt =
        "Ты ассистент маркетплейса Market.tj. Определи что ищет пользователь, " +
        "вызови search_products с ключевым словом, и верни СТРОГО JSON без markdown: " +
        "{\"intent\":\"product|category|cart|orders|none\",\"productId\":null,\"categoryId\":null,\"message\":\"\"}. " +
        "product — один явный товар. category — несколько товаров одной категории. " +
        "cart/orders — если просит корзину/заказы. none — если не понял, message должен объяснить.";

    private const string FarmerSystemPrompt =
        "Ты ассистент маркетплейса Market.tj для ФЕРМЕРА (продавца), уже авторизованного " +
        "в системе. Инструменты: get_dashboard — сводка по моим товарам/заказам/выручке; " +
        "get_my_listings — список МОИХ объявлений (можно фильтровать по статусу); " +
        "propose_update_listing — предложить изменить цену или статус ОДНОГО из МОИХ " +
        "объявлений (сам ничего не меняет — только предлагает фермеру подтвердить, " +
        "используй его как только фермер просит что-то изменить). Всегда вызывай " +
        "подходящий инструмент, если вопрос требует данных. После ответа get_dashboard " +
        "или get_my_listings верни СТРОГО JSON без markdown: {\"intent\":\"info\",\"message\":" +
        "\"<краткий ответ на языке пользователя по полученным данным>\"}. Если инструмент не " +
        "нужен — тоже верни {\"intent\":\"info\",\"message\":\"...\"}.";

    private const string AdminSystemPrompt =
        "Ты ассистент маркетплейса Market.tj для АДМИНИСТРАТОРА, уже авторизованного " +
        "в системе. Инструменты: get_dashboard — сводка по всей платформе (заказы, " +
        "выручка, пользователи); get_pending_verifications — фермеры, ожидающие проверки; " +
        "get_pending_reports — жалобы на объявления, ожидающие рассмотрения; " +
        "propose_resolve_report — предложить рассмотреть жалобу (Reviewed) или отклонить " +
        "(Dismissed) — сам ничего не меняет, только предлагает админу подтвердить. Всегда " +
        "вызывай подходящий инструмент, если вопрос требует данных. После ответа " +
        "get_dashboard/get_pending_verifications/get_pending_reports верни СТРОГО JSON без " +
        "markdown: {\"intent\":\"info\",\"message\":\"<краткий ответ на языке пользователя по " +
        "полученным данным>\"}. Если инструмент не нужен — тоже верни {\"intent\":\"info\"," +
        "\"message\":\"...\"}.";

    public async Task<Result<AssistantResponseDto>> AskAsync(string message)
    {
        try
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogError("Gemini:ApiKey не задан (appsettings.json / User Secrets)");
                return Result<AssistantResponseDto>.Fail("AI-ассистент временно недоступен", ErrorType.InternalServerError);
            }

            var role = currentUser.Role;
            var (systemPrompt, tools) = BuildPromptAndTools(role);

            var contents = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = message } }
                }
            };

            var response = await SendToGeminiAsync(apiKey, systemPrompt, tools, contents);
            var parts = GetFirstCandidateParts(response);

            var functionCallPart = parts?.FirstOrDefault(p => p!["functionCall"] is not null);

            if (functionCallPart is not null)
            {
                var functionCall = functionCallPart["functionCall"]!;
                var functionName = functionCall["name"]!.GetValue<string>();
                var args = functionCall["args"];

                // propose_* — предложение действия формируется сразу, без второго
                // обращения к Gemini: модель просто должна была вызвать инструмент
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

                contents.Add(new JsonObject
                {
                    ["role"] = "model",
                    ["parts"] = parts!.DeepClone()
                });
                contents.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["functionResponse"] = new JsonObject
                            {
                                ["name"] = functionName,
                                ["response"] = new JsonObject { ["content"] = toolResultText }
                            }
                        }
                    }
                });

                response = await SendToGeminiAsync(apiKey, systemPrompt, tools, contents);
                parts = GetFirstCandidateParts(response);
            }

            var textPart = parts?.FirstOrDefault(p => p!["text"] is not null);
            if (textPart is null)
            {
                logger.LogError("Gemini не вернул текстовый ответ: {Response}", response.ToJsonString());
                return Result<AssistantResponseDto>.Fail("Не удалось получить ответ ассистента", ErrorType.InternalServerError);
            }

            var json = textPart["text"]!.GetValue<string>().Trim().Trim('`');
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
            return (FarmerSystemPrompt, new JsonArray { new JsonObject { ["function_declarations"] = tools } });
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
            return (AdminSystemPrompt, new JsonArray { new JsonObject { ["function_declarations"] = tools } });
        }

        // Покупатель, курьер или гость (без токена) — тот же customer-flow, что и раньше.
        return (CustomerSystemPrompt, new JsonArray { new JsonObject { ["function_declarations"] = new JsonArray { customerTool } } });
    }

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
            ["name"] = name,
            ["description"] = description,
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required
            }
        };
    }

    // === Инструменты только на чтение (идут во второй запрос к Gemini) ===

    private async Task<string> ExecuteReadToolAsync(string functionName, JsonNode? args)
        => functionName switch
        {
            "search_products" => await ExecuteSearchProductsAsync(args),
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

    // === Gemini HTTP ===

    private static JsonArray? GetFirstCandidateParts(JsonObject response)
        => response["candidates"]?.AsArray().FirstOrDefault()?["content"]?["parts"]?.AsArray();

    private async Task<JsonObject> SendToGeminiAsync(string apiKey, string systemPrompt, JsonArray tools, JsonArray contents)
    {
        var requestBody = new JsonObject
        {
            ["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray { new JsonObject { ["text"] = systemPrompt } }
            },
            ["tools"] = tools.DeepClone(),
            ["contents"] = contents.DeepClone()
        };

        var url = string.Format(ApiUrlTemplate, Model);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
        };
        // Заголовок вместо ?key=... в URL — HttpClientFactory логирует полный
        // URI запроса на уровне Information, ключ в query string утёк бы в логи.
        request.Headers.Add("x-goog-api-key", apiKey);

        using var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Gemini API вернул {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Gemini API error {response.StatusCode}");
        }

        return JsonNode.Parse(responseBody)!.AsObject();
    }
}
