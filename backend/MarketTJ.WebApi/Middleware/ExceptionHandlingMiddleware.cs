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
            logger.LogError(ex, "Необработанное исключение при обработке {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // В Production клиенту не отдаём детали исключения/stack trace —
            // только общее сообщение.
            var message = environment.IsDevelopment()
                ? ex.Message
                : "Произошла внутренняя ошибка сервера";

            var payload = JsonSerializer.Serialize(new
            {
                statusCode = context.Response.StatusCode,
                message
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
