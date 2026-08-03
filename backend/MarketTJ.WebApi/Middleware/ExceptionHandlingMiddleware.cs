using System.Text.Json;

namespace MarketTJ.WebApi.Middleware;

// Глобальный перехват необработанных исключений — последний рубеж защиты
// поверх try/catch в каждом сервисе (раздел 20 ТЗ). Регистрируется самым
// первым в pipeline (см. Program.cs), чтобы ловить исключения из всех
// последующих middleware и контроллеров.
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{TraceId}] Необработанное исключение при обработке {Method} {Path}", context.TraceIdentifier, context.Request.Method, context.Request.Path);

            // Если ответ уже начал отправляться (заголовки ушли, часть тела
            // записана) — StatusCode/ContentType менять поздно, это бросило
            // бы второе необработанное исключение поверх первого, оставляя
            // клиента с оборванным телом ("Unexpected end of JSON input").
            // В этом случае самое безопасное — не трогать response и просто
            // залогировать (уже сделано выше), дав соединению закрыться как
            // есть, а не пытаться дописать JSON поверх частично отправленного.
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // В Production клиенту не отдаём детали исключения/stack trace —
            // только общее сообщение + traceId, по которому можно найти эту
            // же ошибку в логах Railway (грепом по [{TraceId}] в RequestLoggingMiddleware).
            var message = environment.IsDevelopment()
                ? ex.Message
                : "Произошла внутренняя ошибка сервера";

            var payload = JsonSerializer.Serialize(new
            {
                statusCode = context.Response.StatusCode,
                message,
                traceId = context.TraceIdentifier
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
