using MarketTJ.Application;
using MarketTJ.Infrastructure;
using MarketTJ.Infrastructure.Persistence;
using MarketTJ.Infrastructure.Persistence.Seed;
using MarketTJ.WebApi.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Раздел 17: файлы объявлений хранятся в wwwroot/uploads/listings/{listingId}/.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "listings"));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();

// CORS для React-фронтенда (Frontend/, Vite dev server) — origin'ы берутся
// из конфига (Cors:AllowedOrigins), а не хардкодятся, т.к. в проде адрес
// фронтенда будет другим. AllowCredentials не включаем — Auth (JWT/cookies)
// в проекте ещё не реализован (Этап 2, раздел 23 ТЗ), запросы идут без cookies.
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "Market.tj API", Version = "v1" });
    // JWT Bearer security definition добавится вместе с Authentication (Этап 2, раздел 23) —
    // пока endpoint'ов с [Authorize] нет, описывать схему безопасности рано.
});

var app = builder.Build();

// Применяет накопленные миграции при старте. Пока миграций нет — просто no-op.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}

await Seeder.SeedAsync(app.Services);

// ExceptionHandling — самым первым в pipeline, чтобы ловить исключения из
// всех последующих middleware/контроллеров. RequestLogging — сразу после,
// чтобы в лог запроса попадал в том числе статус-код, который расставил
// ExceptionHandling при необработанном исключении.
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Market.tj API v1");
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
