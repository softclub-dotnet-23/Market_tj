# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Копируем файлы проектов
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

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "MarketTJ.WebApi.dll"]