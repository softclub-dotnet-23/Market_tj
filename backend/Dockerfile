# syntax=docker/dockerfile:1
# Build-контекст — корень репозитория (Railway CLI аплоадит только от git-root,
# --path-as-root на текущей версии CLI нестабильно падал на upload'е), поэтому
# все COPY-пути ниже с префиксом backend/. Локально (docker-compose) контекст
# тоже выставлен в "." с dockerfile: backend/Dockerfile — тот же файл для обоих
# окружений, а не форк под каждую платформу.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — для кэширования слоя restore между сборками.
COPY backend/MarketTJ.WebApi/MarketTJ.WebApi.csproj MarketTJ.WebApi/
COPY backend/MarketTJ.Application/MarketTJ.Application.csproj MarketTJ.Application/
COPY backend/MarketTJ.Infrastructure/MarketTJ.Infrastructure.csproj MarketTJ.Infrastructure/
COPY backend/MarketTJ.Domain/MarketTJ.Domain.csproj MarketTJ.Domain/
RUN dotnet restore MarketTJ.WebApi/MarketTJ.WebApi.csproj

COPY backend/ .
WORKDIR /src/MarketTJ.WebApi
RUN dotnet publish MarketTJ.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Раздел 17 ТЗ: файлы объявлений в wwwroot/uploads/listings — каталог создаёт
# сам Program.cs при старте, монтировать volume под него не обязательно для MVP.
# Порт слушается динамически в Program.cs через переменную PORT (Railway
# подставляет разный порт на каждый деплой) с дефолтом 8080, если PORT не
# задана (docker-compose, dotnet run) — поэтому ASPNETCORE_URLS тут не фиксируется.
EXPOSE 8080

ENTRYPOINT ["dotnet", "MarketTJ.WebApi.dll"]
