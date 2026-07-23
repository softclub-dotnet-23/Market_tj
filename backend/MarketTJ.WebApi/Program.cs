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

var builder = WebApplication.CreateBuilder(args);

// Раздел 17: файлы объявлений хранятся в wwwroot/uploads/listings/{listingId}/.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "listings"));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();

// CORS для React-фронтенда (Frontend/, Vite dev server) — origin'ы берутся
// из конфига (Cors:AllowedOrigins), а не хардкодятся, т.к. в проде адрес
// фронтенда будет другим. AllowCredentials не включаем — токен передаётся
// через Authorization header, а не через cookies.
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
