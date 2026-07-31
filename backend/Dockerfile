# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Build-контекст — корень репозитория (docker-compose.yml: context: ".",
# dockerfile: "backend/Dockerfile"; Railway CLI тоже аплоадит от git-root),
# поэтому все COPY-пути ниже с префиксом backend/.
COPY backend/MarketTJ.Domain/MarketTJ.Domain.csproj MarketTJ.Domain/
COPY backend/MarketTJ.Application/MarketTJ.Application.csproj MarketTJ.Application/
COPY backend/MarketTJ.Infrastructure/MarketTJ.Infrastructure.csproj MarketTJ.Infrastructure/
COPY backend/MarketTJ.WebApi/MarketTJ.WebApi.csproj MarketTJ.WebApi/

# Восстанавливаем NuGet-пакеты
RUN dotnet restore MarketTJ.WebApi/MarketTJ.WebApi.csproj

# Копируем весь backend
COPY backend/ .

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
