# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Build-контекст — папка backend/ (Railway: rootDirectory сервиса выставлен
# в "/backend" в настройках самого сервиса — подтверждено фактическим
# успешным деплоем с этими же путями; docker-compose.yml ниже настроен на
# тот же context: ./backend, чтобы совпадало с одним и тем же Dockerfile).
COPY MarketTJ.Domain/MarketTJ.Domain.csproj MarketTJ.Domain/
COPY MarketTJ.Application/MarketTJ.Application.csproj MarketTJ.Application/
COPY MarketTJ.Infrastructure/MarketTJ.Infrastructure.csproj MarketTJ.Infrastructure/
COPY MarketTJ.WebApi/MarketTJ.WebApi.csproj MarketTJ.WebApi/

# Восстанавливаем NuGet-пакеты
RUN dotnet restore MarketTJ.WebApi/MarketTJ.WebApi.csproj

# Копируем весь backend
COPY . .

# Публикуем Web API
RUN dotnet publish MarketTJ.WebApi/MarketTJ.WebApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Этап запуска
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "MarketTJ.WebApi.dll"]
