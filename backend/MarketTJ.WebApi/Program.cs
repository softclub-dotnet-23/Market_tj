using System.Text;
using MarketTJ.Application;
using MarketTJ.Infrastructure;
using MarketTJ.Infrastructure.Persistence;
using MarketTJ.Infrastructure.Persistence.Seed;
using MarketTJ.Application.Interfaces.Services;
using MarketTJ.WebApi.Middleware;
using MarketTJ.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

// Раздел 17: файлы объявлений хранятся в wwwroot/uploads/listings/{listingId}/.
// Создаётся ДО CreateBuilder — хост резолвит WebRootPath по факту
// существования wwwroot на диске в момент своего построения, а не лениво.
// На Railway контейнер стартует с чистого /app, поэтому создание каталога
// ПОСЛЕ CreateBuilder оставляло WebRootPath как "not found" и UseStaticFiles()
// переставал отдавать файлы вообще (локально маскировалось тем, что каталог
// уже существовал с предыдущих запусков).
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "listings"));

var builder = WebApplication.CreateBuilder(args);

// Railway передаёт порт через PORT (значение каждый раз разное), а не через
// ASPNETCORE_URLS — в контейнере (Railway/docker-compose) переменной либо нет
// (docker-compose), либо есть (Railway), но в обоих случаях это Production,
// поэтому дефолт 8080 корректен (Dockerfile EXPOSE, docker-compose "5000:8080").
// В Development (обычный "dotnet run" с рабочего стола) UseUrls пропускаем —
// иначе он перекрывает applicationUrl из launchSettings.json (5193/7099), на
// который жёстко рассчитан локальный фронтенд (VITE_API_BASE_URL), и рвёт
// связь backend/frontend при локальной разработке.
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://+:{port}");
}

// Railway Postgres даёт DATABASE_URL в виде URI (postgres://user:pass@host:port/db),
// а Npgsql ждёт key=value строку — конвертируем и подкладываем обратно в
// конфигурацию под тем же ключом, которым пользуется AddInfrastructureServices,
// чтобы не менять сигнатуру DI-регистрации ради одного окружения.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    builder.Configuration["ConnectionStrings:DefaultConnection"] =
        $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};" +
        $"Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();

// CORS для React-фронтенда (Frontend/, Vite dev server) — origin'ы берутся
// из конфига (Cors:AllowedOrigins), а не хардкодятся, т.к. в проде адрес
// фронтенда будет другим. AllowCredentials не включаем — токен передаётся
// через Authorization header, а не через cookies.
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []).ToList();

// FRONTEND_URL — адрес задеплоенного на Railway фронтенда; добавляется поверх
// локальных origin'ов из конфига, а не вместо них, чтобы локальная разработка
// не сломалась после деплоя.
var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
if (!string.IsNullOrEmpty(frontendUrl))
{
    allowedOrigins.Add(frontendUrl);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Минимальный login для админа (раздел 23 ТЗ — полноценная Authentication с
// регистрацией Customer/Farmer остаётся отдельным этапом, здесь только JWT
// issue/validate для уже существующих сидированных пользователей).
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// GET /health — для docker-compose healthcheck / внешнего мониторинга,
// без авторизации (см. app.MapHealthChecks ниже — не под UseAuthorization).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Market.tj API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Вставьте только сам JWT-токен (без слова \"Bearer\")."
    });

    // Без этого SecurityDefinition выше только описывает схему в JSON-схеме,
    // но кнопка Authorize в Swagger UI не подставляет токен в запросы —
    // требование должно быть явно привязано к операциям.
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), new List<string>() }
    });
});

var app = builder.Build();

// Применяет накопленные миграции при старте. try/catch — чтобы падение
// миграции (например, недоступна БД на Railway) попало в логи явно, а не
// уронило контейнер молча без объяснения причины.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Migration failed");
        throw;
    }
}

// Audit 2026-07-28, находка 2.1 — защитная миграция на случай, если через
// уязвимый POST/PUT /api/users уже успели создать/обновить пользователя с
// нехэшированным паролем до применения фикса (см. PlaintextPasswordFixup).
await PlaintextPasswordFixup.RunAsync(app.Services);

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
