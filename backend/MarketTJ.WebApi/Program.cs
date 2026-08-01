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

Directory.CreateDirectory(
    Path.Combine(
        Directory.GetCurrentDirectory(),
        "wwwroot",
        "uploads",
        "listings"
    )
);

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Railway PostgreSQL connection
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);

    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1
        ? Uri.UnescapeDataString(userInfo[1])
        : string.Empty;

    var database = uri.LocalPath.TrimStart('/');

    builder.Configuration["ConnectionStrings:DefaultConnection"] =
        $"Host={uri.Host};" +
        $"Port={uri.Port};" +
        $"Database={database};" +
        $"Username={username};" +
        $"Password={password};" +
        $"SSL Mode=Require;" +
        $"Trust Server Certificate=true";
}

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddControllers();

const string FrontendCorsPolicy = "FrontendCorsPolicy";

var allowedOrigins =
    (builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [])
    .ToList();

var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");

if (!string.IsNullOrWhiteSpace(frontendUrl))
{
    allowedOrigins.Add(frontendUrl.TrimEnd('/'));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        if (allowedOrigins.Count > 0)
        {
            policy
                .WithOrigins(allowedOrigins.Distinct().ToArray())
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else if (builder.Environment.IsDevelopment())
        {
            // Только для локальной разработки — порт Vite меняется чаще,
            // чем хочется поддерживать в конфиге вручную.
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            // Production с пустым Cors:AllowedOrigins — это ошибка
            // конфигурации (забыли FRONTEND_URL/Cors:AllowedOrigins), а не
            // сигнал "разреши всем". Fail closed (CORS не разрешит ни один
            // origin) + громкий warning в лог вместо тихого wildcard.
            Console.WriteLine(
                "[WARN] Cors:AllowedOrigins пуст в Production — CORS не разрешит ни один " +
                "origin, пока не будет задан FRONTEND_URL или Cors__AllowedOrigins__0."
            );
        }
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "JWT Secret is missing. Add Jwt__Secret in Railway Variables."
    );
}

// TokenService.AccessTokenExpiryMinutes делает int.Parse(...) без проверки —
// раньше это падало необработанным исключением при первом логине, а не при
// старте приложения. Проверяем здесь по той же схеме, что и Jwt:Secret выше.
if (!int.TryParse(jwtSection["ExpiryMinutes"], out _))
{
    throw new InvalidOperationException(
        "Jwt:ExpiryMinutes is missing or not a valid integer. Add Jwt__ExpiryMinutes in Railway Variables (or appsettings.json locally)."
    );
}

// Раньше отсутствие SMTP всплывало только на первом реальном запросе
// (500 на POST /api/auth/send-verification-code, глубоко внутри
// SmtpEmailSender) — проверяем при старте, чтобы ошибка была видна сразу
// в логах деплоя, а не после первой жалобы пользователя на регистрацию.
// ВАЖНО: только предупреждение, никогда не throw — SMTP нужен только для
// одной фичи (подтверждение email при регистрации), а не для работы всего
// backend целиком. Безусловный throw здесь однажды уже уронил весь сервис
// на Railway при отсутствии реальных Smtp-переменных (2026-07-31).
var smtpSection = builder.Configuration.GetSection("Smtp");
var smtpConfigured = !string.IsNullOrWhiteSpace(smtpSection["Host"])
    && !string.IsNullOrWhiteSpace(smtpSection["User"])
    && !string.IsNullOrWhiteSpace(smtpSection["Password"]);

if (!smtpConfigured)
{
    Console.WriteLine(
        "[WARN] SMTP не настроен (Smtp:Host/Port/User/Password/From) — подтверждение email при " +
        "регистрации работать не будет (остальной backend продолжит работать нормально). " +
        "Задайте Smtp__Host, Smtp__Port, Smtp__User, Smtp__Password, Smtp__From через переменные " +
        "окружения (Railway) или dotnet user-secrets (локально)."
    );
}

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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)
            ),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Market.tj API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Вставьте только JWT-токен без слова Bearer."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

var app = builder.Build();

app.Logger.LogInformation("Starting database migration...");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Database migration completed.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Migration failed");
        throw;
    }
}

// Audit 2026-07-28, находка 2.1 — защитная миграция: деактивирует аккаунты
// с нехэшированным паролем, если такие успели попасть в базу до фикса.
await PlaintextPasswordFixup.RunAsync(app.Services);
await Seeder.SeedAsync(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Swagger включён и в Production
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Market.tj API v1"
    );
});

app.UseStaticFiles();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.MapGet("/", () => Results.Ok(new
{
    application = "Market.tj API",
    status = "running",
    swagger = "/swagger",
    health = "/health"
}));

app.Logger.LogInformation("MarketTJ API is starting...");

app.Run();