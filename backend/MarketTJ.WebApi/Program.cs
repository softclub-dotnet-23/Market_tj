using MarketTJ.Application;
using MarketTJ.Infrastructure;
using MarketTJ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Раздел 17: файлы объявлений хранятся в wwwroot/uploads/listings/{listingId}/.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "listings"));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
