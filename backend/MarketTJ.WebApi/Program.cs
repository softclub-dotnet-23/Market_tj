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
builder.Services.AddInfrastructureServices(builder.Configuration);

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
        else
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
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

// Временно отключены для проверки запуска Railway.
// Когда API стабильно заработает, можно включить обратно.
//
// await PlaintextPasswordFixup.RunAsync(app.Services);
// await Seeder.SeedAsync(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Swagger включён и в Production
app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Market.tj API v1");
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