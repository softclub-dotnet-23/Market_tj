# syntax=docker/dockerfile:1
# Build-контекст — backend/ (нужен доступ к соседним проектам через ProjectReference:
# MarketTJ.Application, MarketTJ.Infrastructure, MarketTJ.Domain).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — для кэширования слоя restore между сборками.
COPY MarketTJ.WebApi/MarketTJ.WebApi.csproj MarketTJ.WebApi/
COPY MarketTJ.Application/MarketTJ.Application.csproj MarketTJ.Application/
COPY MarketTJ.Infrastructure/MarketTJ.Infrastructure.csproj MarketTJ.Infrastructure/
COPY MarketTJ.Domain/MarketTJ.Domain.csproj MarketTJ.Domain/
RUN dotnet restore MarketTJ.WebApi/MarketTJ.WebApi.csproj

COPY . .
WORKDIR /src/MarketTJ.WebApi
RUN dotnet publish MarketTJ.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Раздел 17 ТЗ: файлы объявлений в wwwroot/uploads/listings — каталог создаёт
# сам Program.cs при старте, монтировать volume под него не обязательно для MVP.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MarketTJ.WebApi.dll"]
